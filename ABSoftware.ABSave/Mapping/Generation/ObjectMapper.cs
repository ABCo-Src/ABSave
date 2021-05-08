using ABSoftware.ABSave.Exceptions;
using ABSoftware.ABSave.Helpers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace ABSoftware.ABSave.Mapping.Generation
{
    /// <summary>
    /// Creates a final map of an object using intermediate information from <see cref="GenObjectTranslator"/>
    /// </summary>
    internal static class ObjectMapper
    {
        struct Context
        {
            public int TargetVersion;
            public IntermediateObjInfo Intermediate;
            public MapGenerator Gen;

            public Context(int targetVersion, IntermediateObjInfo info, MapGenerator gen)
                => (TargetVersion, Intermediate, Gen) = (targetVersion, info, gen);
        }

        internal static ObjectMemberInfo[]? GetVersionOrAddNull(int version, ref ObjectMapItem parentObj)
        {
            while (true)
            {
                lock (parentObj.Versions)
                {
                    // Does not exist - Has not and is not generating.
                    // Exists but is null - Is currently generating.
                    // Exists and is not null - Is ready to use.
                    if (parentObj.Versions.TryGetValue(version, out ObjectMemberInfo[]? info))
                    {
                        if (info != null) return info;
                    }
                    else
                    {
                        parentObj.Versions.Add(version, null);
                        return null;
                    }
                }

                Thread.Yield();
            }
        }

        internal static ObjectMemberInfo[] GenerateVersion(MapGenerator gen, int version, ref MapItem parent, ref ObjectMapItem parentObj)
        {
            int latestVer = parent.Extra.ObjectHighestVersion;

            if (version > latestVer)
                throw new UnsupportedVersionException(parent.ItemType, version);

            var ctx = new Context(version, parentObj.IntermediateInfo!, gen);
            var newVer = GenerateNewVersion(ref ctx, ref parent);
           
            lock (parentObj.Versions)
            {
                parentObj.Versions[version] = newVer;
                if (parentObj.Versions.Count > parent.Extra.ObjectHighestVersion)
                {
                    IntermediateObjInfoMapper.Release(parentObj.IntermediateInfo!);
                    parentObj.IntermediateInfo = null;
                }
            }
                
            return newVer;
        }

        internal static void GenerateNewObject(Type type, MapGenerator gen, MapItemInfo dest)
            => GenerateNewObject(IntermediateObjInfoMapper.CreateInfo(type, gen), gen, dest);

        internal static void GenerateNewObject(IntermediateObjInfo info, MapGenerator gen, MapItemInfo dest)
        {
            gen.Map.Items.EnsureCapacity(info.UnmappedCount + 1);

            // Create the item
            ref MapItem parent = ref gen.FillItemWith(MapItemType.Object, dest);
            ref ObjectMapItem parentObj = ref parent.Main.Object;

            try
            {
                // If there's literally zero members, just do nothing.
                if (info.RawMembers.Length == 0)
                {
                    parentObj.IntermediateInfo = null;
                    IntermediateObjInfoMapper.Release(info);

                    parentObj.Versions = new Dictionary<int, ObjectMemberInfo[]?>() { { 0, Array.Empty<ObjectMemberInfo>() } };
                    parent.Extra.ObjectHighestVersion = 0;
                    return;
                }

                parent.Extra.ObjectHighestVersion = info.HighestVersion;

                // Handle objects with only one version quickly without any extra work.
                if (info.HighestVersion == 0)
                {
                    var ctx = new Context(0, info, gen);
                    GenerateForOneVersion(ref ctx, ref parent, ref parentObj);

                    // There are no more versions here, release the translated.
                    parentObj.IntermediateInfo = null;
                    IntermediateObjInfoMapper.Release(info);
                }
                else
                {
                    parentObj.IntermediateInfo = info;
                    parentObj.Versions = new Dictionary<int, ObjectMemberInfo[]?>();
                }
            }
            finally { parent.IsGenerating = false; }
        }

        static ObjectMemberInfo[] GenerateNewVersion(ref Context ctx, ref MapItem parent)
        {
            var lst = new LoadOnceList<ObjectMemberInfo>();
            lst.Initialize();

            int version = ctx.TargetVersion;
            var iterator = new IntermediateObjInfo.MemberIterator(ctx.Intermediate);

            do
            {
                ref ObjectTranslatedItemInfo tranItm = ref iterator.GetCurrent();

                if (version >= tranItm.StartVer && version <= tranItm.EndVer)
                    CreateItem(ctx.Gen, ref parent, ref lst.CreateAndGet(), ref tranItm);
            }
            while (iterator.MoveNext());

            return lst.ReleaseAndGetArray();
        }

        private static void GenerateForOneVersion(ref Context ctx, ref MapItem parent, ref ObjectMapItem parentObj)
        {
            var iterator = new IntermediateObjInfo.MemberIterator(ctx.Intermediate);

            // No need to do any checks at all - just copy the items right across!
            ObjectMemberInfo[] dest = new ObjectMemberInfo[ctx.Intermediate.RawMembers.Length];

            do
                CreateItem(ctx.Gen, ref parent, ref dest[iterator.Index], ref iterator.GetCurrent());
            while (iterator.MoveNext());

            parentObj.Versions = new Dictionary<int, ObjectMemberInfo[]?>(1) { { ctx.TargetVersion, dest } };
        }

        static void CreateItem(MapGenerator gen, ref MapItem parent, ref ObjectMemberInfo dest, ref ObjectTranslatedItemInfo translated)
        {
            // Get or create the details needed to have a complete map.
            dest.Map = translated.ExistingMap ??= gen.GetMap(translated.MemberType);

            ref MapItem newItem = ref gen.Map.GetItemAt(dest.Map);

            // Create an accessor for the item if there isn't already one.
            dest.Accessor = translated.Accessor
                ??= GenerateAccessor(ref newItem, ref parent, translated.Info);
        }

        // Internal for testing:
        /// <summary>
        /// Generate the fastest possible accessor for this member. See more details on <see cref="MemberAccessor"/>
        /// </summary>
        internal static MemberAccessor GenerateAccessor(ref MapItem item, ref MapItem parentItem, MemberInfo memberInfo)
        {
            var accessor = new MemberAccessor();

            // Field
            if (memberInfo is FieldInfo field)
            {
                accessor.Initialize(memberInfo, null, accessor.FieldGetter, accessor.FieldSetter);
                return accessor;
            }

            // Property - All property optimizations rely on the parent being a reference-type.
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
                var getter = property.GetGetMethod()!.CreateDelegate(GenericPropertyGetterDelegate.MakeGenericType(parentType, type));
                var setter = property.GetSetMethod()!.CreateDelegate(GenericPropertySetterDelegate.MakeGenericType(parentType, type));

                accessor.Initialize(getter, setter, accessor.PrimitiveGetter<T>, accessor.PrimitiveSetter<T>);
                return true;
            }
        }

        private static MemberAccessor GetAllRefTypePropertyAccessor(MemberAccessor accessor, Type parent, MapItem item, PropertyInfo property)
        {
            var propGetter = property.GetGetMethod()!.CreateDelegate(
                GenericPropertyGetterDelegate.MakeGenericType(parent, item.ItemType));

            var propSetter = property.GetSetMethod()!.CreateDelegate(
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
        public Func<object, object?> Getter = null!;
        public Action<object, object?> Setter = null!;

        // These objects are used by the getter and setter as they want. Object2 may be left unused.
        public object Object1 = null!;
        public object? Object2 = null;

        public void Initialize(object obj1, object? obj2, Func<object, object?> getter, Action<object, object?> setter) =>
            (Object1, Object2, Getter, Setter) = (obj1, obj2, getter, setter);

        internal object? FieldGetter(object parent) =>
            ABSaveUtils.UnsafeFastCast<FieldInfo>(Object1).GetValue(parent);

        internal void FieldSetter(object parent, object? value) =>
            ABSaveUtils.UnsafeFastCast<FieldInfo>(Object1).SetValue(parent, value);

        internal object? SlowGetter(object parent) =>
            ABSaveUtils.UnsafeFastCast<PropertyInfo>(Object1).GetValue(parent);

        internal void SlowSetter(object parent, object? value) =>
            ABSaveUtils.UnsafeFastCast<PropertyInfo>(Object1).SetValue(parent, value);

        internal object? PrimitiveGetter<T>(object parent) =>
            Unsafe.As<Func<object, T>>(Object1)(parent);

        internal void PrimitiveSetter<T>(object parent, object? value) where T : struct =>
            Unsafe.As<Action<object, T>>(Object2!)(parent, (T)value!);

        internal object? AllRefGetter(object parent) =>
            Unsafe.As<Func<object, object?>>(Object1)(parent);

        internal void AllRefSetter(object parent, object? value) =>
            Unsafe.As<Action<object, object?>>(Object2!)(parent, value);
    }
}
