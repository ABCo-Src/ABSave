using ABSoftware.ABSave.Helpers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace ABSoftware.ABSave.Mapping.Generation
{
    /// <summary>
    /// Creates a final map of an object using intermediate information from <see cref="GenObjectReflector"/>
    /// </summary>
    internal static class GenObject
    {        
        internal static void GenerateObject(ref ObjectReflectorInfo memberInfo, MapGenerator gen, MapItemInfo dest)
        {
            gen.Map.Items.EnsureCapacity(memberInfo.UnmappedMembers);

            ref MapItem item = ref gen.FillItemWith(MapItemType.Object, dest);
            ref ObjectMapItem objItem = ref MapItem.GetObjectData(ref item);

            string[] keys = new string[memberInfo.Members.Length];
            objItem.Members = new ObjectMemberInfo[memberInfo.Members.Length];

            // Move the members to the map
            for (int i = 0; i < memberInfo.Members.Length; i++)
            {
                ref ObjectReflectorItemInfo currentInfo = ref memberInfo.Members[i];
                keys[i] = currentInfo.NameKey;

                // Get or create the details needed to have a complete map.
                objItem.Members[i].Map = currentInfo.ExistingMap ??= gen.GetMap(memberInfo.Members[i].MemberType);

                ref MapItem currentItem = ref gen.Map.GetItemAt(objItem.Members[i].Map);

                // Create an accessor for the item if there isn't already one.
                objItem.Members[i].Accessor = memberInfo.Members[i].Accessor
                    ??= GenerateAccessor(gen, ref currentItem, ref item, memberInfo.Members[i].Info);
            }

            item.IsGenerating = false;

            // Sort the items by their names.
            Array.Sort(keys, objItem.Members);
        }

        // Internal for testing:
        /// <summary>
        /// Generate the fastest possible accessor for this member. See more details on <see cref="MemberAccessor"/>
        /// </summary>
        internal static MemberAccessor GenerateAccessor(MapGenerator gen, ref MapItem item, ref MapItem parentItem, MemberInfo memberInfo)
        {
            var accessor = new MemberAccessor();

            // Fields
            if (GenObjectReflector.IsField(gen))
            {
                accessor.Initialize(memberInfo, null, accessor.FieldGetter, accessor.FieldSetter);
                return accessor;
            }

            // All property optimizations rely on the parent being a reference-type.
            else if (!parentItem.IsValueType)
            {
                var property = ABSaveUtils.UnsafeFastCast<PropertyInfo>(memberInfo);

                // Value type property - Just support some basic primitives.
                if (item.IsValueType)
                {
                    // Rationale: Generating an accessor for value types is very difficult, as JIT gen
                    // for value types varies wildly and is too implementation-specific. Since this 
                    // is only designed to be a quick and dirty fast accessor anyway, it's easiest if
                    // we only support the very basic primitives (which are very common anyway)
                    // and nothing more. "MakeGenericMethod" is too expensive.
                    var successful = TryGenerateAccessorPrimitive(accessor, parentItem.ItemType, item.ItemType, property);
                    if (successful) return accessor;
                }

                // Reference type property - Simply force cast to "object".
                else
                    return GetAllRefTypePropertyAccessor(accessor, parentItem.ItemType, item, property);
            }

            // Unoptimized
            accessor.Initialize(memberInfo, null, accessor.SlowGetter, accessor.SlowSetter);
            return accessor;
        }

        static readonly Type GenericPropertyGetterDelegate = typeof(Func<,>);
        static readonly Type GenericPropertySetterDelegate = typeof(Action<,>);

        // Try to generate an accessor for a built-in primitive type.
        static bool TryGenerateAccessorPrimitive(MemberAccessor accessor, Type parentType, Type type, PropertyInfo property)
        {
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Boolean => ApplyPrimitiveAccessorFor<bool>(),
                TypeCode.Byte => ApplyPrimitiveAccessorFor<byte>(),
                TypeCode.SByte => ApplyPrimitiveAccessorFor<sbyte>(),
                TypeCode.UInt16 => ApplyPrimitiveAccessorFor<ushort>(),
                TypeCode.Int16 => ApplyPrimitiveAccessorFor<short>(),
                TypeCode.Int32 => ApplyPrimitiveAccessorFor<int>(),
                TypeCode.UInt32 => ApplyPrimitiveAccessorFor<uint>(),
                TypeCode.Int64 => ApplyPrimitiveAccessorFor<long>(),
                TypeCode.UInt64 => ApplyPrimitiveAccessorFor<ulong>(),
                TypeCode.Single => ApplyPrimitiveAccessorFor<float>(),
                TypeCode.Double => ApplyPrimitiveAccessorFor<double>(),
                TypeCode.Decimal => ApplyPrimitiveAccessorFor<decimal>(),
                TypeCode.DateTime => ApplyPrimitiveAccessorFor<DateTime>(),
                _ => false
            };

            bool ApplyPrimitiveAccessorFor<T>() where T : struct
            {
                var getter = property.GetGetMethod().CreateDelegate(GenericPropertyGetterDelegate.MakeGenericType(parentType, type));
                var setter = property.GetSetMethod().CreateDelegate(GenericPropertySetterDelegate.MakeGenericType(parentType, type));

                accessor.Initialize(getter, setter, accessor.PrimitiveGetter<T>, accessor.PrimitiveSetter<T>);
                return true;
            }
        }

        private static MemberAccessor GetAllRefTypePropertyAccessor(MemberAccessor accessor, Type parent, MapItem item, PropertyInfo property)
        {
            var propGetter = property.GetGetMethod().CreateDelegate(
                GenericPropertyGetterDelegate.MakeGenericType(parent, item.ItemType));

            var propSetter = property.GetSetMethod().CreateDelegate(
                GenericPropertySetterDelegate.MakeGenericType(parent, item.ItemType));

            accessor.Initialize(propGetter, propSetter, accessor.AllRefGetter, accessor.AllRefSetter);
            return accessor;
        }
    }

    /// <summary>
    /// Represents the best getter and setter for a member. 
    /// The getting/setting method used should be relatively cheap to generate, but make a significant
    /// improvement on the time spent at serialization-time accessing members. The code-gen will 
    /// produce a native mechanism in the end anyway, these should just be quick and dirty ways of speeding
    /// up un-optimized accesses for the initial runs.
    /// </summary>
    internal class MemberAccessor
    {
        public Func<object, object> Getter;
        public Action<object, object> Setter;

        // These objects are used by the getter and setter as they want.
        public object Object1;
        public object Object2;

        public void Initialize(object obj1, object obj2, Func<object, object> getter, Action<object, object> setter) =>
            (Object1, Object2, Getter, Setter) = (obj1, obj2, getter, setter);

        internal object FieldGetter(object parent) =>
            ABSaveUtils.UnsafeFastCast<FieldInfo>(Object1).GetValue(parent);

        internal void FieldSetter(object parent, object value) =>
            ABSaveUtils.UnsafeFastCast<FieldInfo>(Object1).SetValue(parent, value);

        internal object SlowGetter(object parent) =>
            ABSaveUtils.UnsafeFastCast<PropertyInfo>(Object1).GetValue(parent);

        internal void SlowSetter(object parent, object value) =>
            ABSaveUtils.UnsafeFastCast<PropertyInfo>(Object1).SetValue(parent, value);

        internal object PrimitiveGetter<T>(object parent) =>
            Unsafe.As<Func<object, T>>(Object1)(parent);

        internal void PrimitiveSetter<T>(object parent, object value) where T : struct
            => Unsafe.As<Action<object, T>>(Object2)(parent, Unsafe.Unbox<T>(value));

        internal object AllRefGetter(object parent) =>
            Unsafe.As<Func<object, object>>(Object1)(parent);

        internal void AllRefSetter(object parent, object value) =>
            Unsafe.As<Action<object, object>>(Object2)(parent, value);
    }
}
