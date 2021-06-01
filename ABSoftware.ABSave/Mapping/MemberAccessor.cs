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
                // MemberAccessorType.PrimitiveProperty => Unsafe.As<Func<object, T>>(Object1)(parent);
                _ => throw new Exception("Unrecognized member accessor type"),
            };
        }

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
                default:
                    throw new Exception("Unrecognized member accessor type");
            };
        }

        public void Initialize(MemberAccessorType type, object obj1, object? obj2) =>
            (Type, Object1, Object2) = (type, obj1, obj2);
    }

    internal enum MemberAccessorType
    {
        Field,

        // An unoptimized property accessor
        SlowProperty,

        // A primitive property accessor (with a reference type parent)
        //PrimitiveProperty,

        // An all-reference-type property accessor
        AllRefProperty
    }
}
