using ABSoftware.ABSave.Helpers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace ABSoftware.ABSave.Mapping
{
    /// <summary>
    /// Represents the best getter and setter for a member. 
    /// The getting/setting method used should be relatively cheap to generate, but make a significant
    /// improvement on the time spent at serialization-time accessing members. The code-gen will 
    /// produce a native mechanism in the end anyway, these should just be quick and dirty ways of speeding
    /// up un-optimized accesses for the initial runs.
    /// </summary>
    internal struct MemberAccessor
    {
        public MemberAccessorType Type;
        public TypeCode PrimitiveTypeCode;

        // These objects are used by the getter and setter as they want. Object2 may be left unused.
        public object Object1;
        public object? Object2;

        public object? Getter(object parent)
        {
            return Type switch
            {
                MemberAccessorType.Field => ((FieldInfo)Object1).GetValue(parent),
                MemberAccessorType.SlowProperty => ((PropertyInfo)Object1).GetValue(parent),
                MemberAccessorType.AllRefProperty => Unsafe.As<Func<object, object?>>(Object1)(parent),
                MemberAccessorType.PrimitiveProperty => PrimitivePropertyGetter(parent),
                _ => throw new Exception("Unrecognized member accessor type"),
            };
        }

        public object PrimitivePropertyGetter(object parent)
        {
            return PrimitiveTypeCode switch
            {
                TypeCode.Boolean => PrimitiveGetter<bool>(parent),
                TypeCode.Byte => PrimitiveGetter<byte>(parent),
                TypeCode.SByte => PrimitiveGetter<sbyte>(parent),
                TypeCode.UInt16 => PrimitiveGetter<ushort>(parent),
                TypeCode.Int16 => PrimitiveGetter<short>(parent),
                TypeCode.Int32 => PrimitiveGetter<int>(parent),
                TypeCode.UInt32 => PrimitiveGetter<uint>(parent),
                TypeCode.Int64 => PrimitiveGetter<long>(parent),
                TypeCode.UInt64 => PrimitiveGetter<ulong>(parent),
                TypeCode.Single => PrimitiveGetter<float>(parent),
                TypeCode.Double => PrimitiveGetter<double>(parent),
                TypeCode.Decimal => PrimitiveGetter<decimal>(parent),
                TypeCode.DateTime => PrimitiveGetter<DateTime>(parent),
                _ => throw new Exception("Invalid type-code for primitive property getter.")
            };
        }

        object PrimitiveGetter<T>(object parent) where T : struct 
            => Unsafe.As<Func<object, T>>(Object1)!(parent);

        public void Setter(object parent, object? value)
        {
            switch (Type)
            {
                case MemberAccessorType.Field:
                    ((FieldInfo)Object1).SetValue(parent, value);
                    break;
                case MemberAccessorType.SlowProperty:
                    ((PropertyInfo)Object1).SetValue(parent, value);
                    break;
                case MemberAccessorType.AllRefProperty:
                    Unsafe.As<Action<object, object?>>(Object2)!(parent, value);
                    break;
                case MemberAccessorType.PrimitiveProperty:
                    PrimitivePropertySetter(parent, value!);
                    break;
                default:
                    throw new Exception("Unrecognized member accessor type");
            };
        }

        public void PrimitivePropertySetter(object parent, object value)
        {
            switch (PrimitiveTypeCode)
            {
                case TypeCode.Boolean:
                    PrimitiveSetter<bool>(parent, value);
                    break;
                case TypeCode.Byte:
                    PrimitiveSetter<byte>(parent, value);
                    break;
                case TypeCode.SByte:
                    PrimitiveSetter<sbyte>(parent, value);
                    break;
                case TypeCode.UInt16:
                    PrimitiveSetter<ushort>(parent, value);
                    break;
                case TypeCode.Int16:
                    PrimitiveSetter<short>(parent, value);
                    break;
                case TypeCode.Int32:
                    PrimitiveSetter<int>(parent, value);
                    break;
                case TypeCode.UInt32:
                    PrimitiveSetter<uint>(parent, value);
                    break;
                case TypeCode.Int64:
                    PrimitiveSetter<long>(parent, value);
                    break;
                case TypeCode.UInt64:
                    PrimitiveSetter<ulong>(parent, value);
                    break;
                case TypeCode.Single:
                    PrimitiveSetter<float>(parent, value);
                    break;
                case TypeCode.Double:
                    PrimitiveSetter<double>(parent, value);
                    break;
                case TypeCode.Decimal:
                    PrimitiveSetter<decimal>(parent, value);
                    break;
                case TypeCode.DateTime:
                    PrimitiveSetter<DateTime>(parent, value);
                    break;
                default:
                    throw new Exception("Invalid type-code for primitive property setter.");
            };
        }

        void PrimitiveSetter<T>(object parent, object value) where T : struct 
            => Unsafe.As<Action<object, T>>(Object2)!(parent, (T)value);

        public void Initialize(MemberAccessorType type, object obj1, object? obj2) =>
            (Type, Object1, Object2) = (type, obj1, obj2);
    }

    internal enum MemberAccessorType
    {
        Field,

        // An unoptimized property accessor
        SlowProperty,

        // A primitive property accessor (with a reference type parent)
        PrimitiveProperty,

        // An all-reference-type property accessor
        AllRefProperty
    }
}
