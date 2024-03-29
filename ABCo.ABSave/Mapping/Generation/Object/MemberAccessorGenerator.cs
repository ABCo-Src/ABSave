﻿using ABCo.ABSave.Serialization.Converters;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ABCo.ABSave.Exceptions;

namespace ABCo.ABSave.Mapping.Generation.Object
{
    internal static class MemberAccessorGenerator
    {
	    internal static void GenerateFieldAccessor(ref MemberAccessor dest, FieldInfo fieldInfo)
	    {
            if(fieldInfo.IsInitOnly) throw new UnsupportedMemberException(fieldInfo, "Field is readonly.");

            dest.Initialize(MemberAccessorType.Field, fieldInfo, null);
        }
	    
        internal struct PropertyToProcess
        {
            public ObjectMemberSharedInfo Info;
            public PropertyInfo Property;
            public Converter Parent;

            public PropertyToProcess(ObjectMemberSharedInfo info, PropertyInfo property, Converter parent) =>
                (Info, Parent, Property) = (info, parent, property);
        }

        internal static void GeneratePropertyAccessor(MapGenerator gen, ObjectMemberSharedInfo info, PropertyInfo property, Converter parent) =>
            // Queue it up to be processed later, where "DoGeneratePropertyAccessor" will be running parallel.
            gen.QueuePropertyForProcessing(new PropertyToProcess(info, property, parent));

        /// <summary>
        /// Generate the fastest possible accessor for the given property. See more details on <see cref="MemberAccessor"/>
        /// </summary>
        internal static void DoGeneratePropertyAccessor(ref MemberAccessor accessor, MapItemInfo item, Converter parentItem, PropertyInfo property)
        {
	        if (!property.CanWrite) throw new UnsupportedMemberException(property, "Property is readonly.");

            // All property optimizations rely on the parent being a reference-type.
            if (!parentItem.IsValueItemType)
            {
                // Value type property - Just support some basic primitives.
                if (item.IsValueTypeItem)
                {
                    // Rationale: Generating an accessor for value types is very difficult, as JIT gen
                    // for value types varies wildly and is too implementation-specific. Since this 
                    // is only designed to be a quick and dirty fast accessor anyway, it's easiest if
                    // we only support the very basic primitives (which are very common anyway)
                    // and nothing more. "MakeGenericMethod" is too expensive.
                    bool successful = TryGenerateAccessorPrimitive(ref accessor, parentItem.ItemType, item.GetItemType(), property);
                    if (successful) return;
                }

                // Reference type property - Simply force cast to "object".
                else
                {
                    CreateAllRefTypeAccessor(ref accessor, parentItem.ItemType, item.GetItemType(), property);
                    return;
                }
            }

            // Unoptimized
            accessor.Initialize(MemberAccessorType.SlowProperty, property, null);
        }

        internal delegate object ReferenceGetterDelegate<T>(T val);
        static readonly Type GenericRefPropertyGetter = typeof(ReferenceGetterDelegate<>);
        static readonly Type GenericPrimitivePropertyGetter = typeof(Func<,>);
        static readonly Type GenericPropertySetter = typeof(Action<,>);

        // Try to generate an accessor for a built-in primitive type.
        static bool TryGenerateAccessorPrimitive(ref MemberAccessor accessor, Type parentType, Type type, PropertyInfo property)
        {
            TypeCode typeCode = Type.GetTypeCode(type);

            // If it's not in the range of supported types (bool, DateTime, primitives) don't do the primitive accessor.
            if (typeCode < TypeCode.Boolean || typeCode > TypeCode.DateTime) return false;

            Delegate? getter = property.GetGetMethod()!.CreateDelegate(GenericPrimitivePropertyGetter.MakeGenericType(parentType, type));
            Delegate? setter = property.GetSetMethod()!.CreateDelegate(GenericPropertySetter.MakeGenericType(parentType, type));

            accessor.Initialize(MemberAccessorType.PrimitiveProperty, getter, setter);
            accessor.PrimitiveTypeCode = typeCode;
            return true;
        }

        static void CreateAllRefTypeAccessor(ref MemberAccessor accessor, Type parent, Type itemType, PropertyInfo property)
        {
            Delegate? propGetter = property.GetGetMethod()!.CreateDelegate(
                GenericRefPropertyGetter.MakeGenericType(parent));

            Delegate? propSetter = property.GetSetMethod()!.CreateDelegate(
                GenericPropertySetter.MakeGenericType(parent, itemType));

            accessor.Initialize(MemberAccessorType.AllRefProperty, propGetter, propSetter);
        }

        internal static void ProcessAllQueuedAccessors(List<PropertyToProcess> properties)
        {
            Parallel.ForEach(properties, s =>
            {
                DoGeneratePropertyAccessor(ref s.Info.Accessor, s.Info.Map, s.Parent, s.Property);
            });

            properties.Clear();
        }
    }
}
