using ABCo.ABSave.Configuration;
using ABCo.ABSave.Converters;
using ABCo.ABSave.Deserialization;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description;
using ABCo.ABSave.Mapping.Description.Attributes;
using ABCo.ABSave.Mapping.Generation;
using ABCo.ABSave.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ABCo.ABSave.Converters
{
    public abstract class Converter : MapItem
    {
        internal SaveInheritanceAttribute[]? _allInheritanceAttributes = null;
        internal Dictionary<uint, SaveInheritanceAttribute?>? _inheritanceValues = null;

        /// <summary>
        /// Initializes a given converter for a given type.
        /// </summary>
        public virtual void Initialize(InitializeInfo info) { }

        /// <summary>
        /// Check whether the converter supports a given type, used for non-exact types.
        /// This method is allowed to modify variables, however it is nt.
        /// </summary>
        public virtual bool CheckType(CheckTypeInfo info) => throw new Exception("Converter says it also converts non-exact but does not override 'CheckType' to check for one.");

        public virtual (ConverterVersionInfo?, bool) GetVersionInfo(uint version) => (null, false);
        public virtual bool UsesHeaderForVersion(uint version) => false;

        public struct SerializeInfo
        {
            public object Instance { get; }
            public Type ActualType { get; }
            internal ConverterVersionInfo _versionInfo;

            internal SerializeInfo(object instance, Type actualType, ConverterVersionInfo versionInfo) => 
                (Instance, ActualType, _versionInfo) = (instance, actualType, versionInfo);
        }

        public abstract void Serialize(in SerializeInfo info, ref BitTarget header);

        public struct DeserializeInfo
        {
            public Type ActualType { get; }
            internal ConverterVersionInfo _versionInfo;

            internal DeserializeInfo(Type actualType, ConverterVersionInfo versionInfo) => 
                (ActualType, _versionInfo) = (actualType, versionInfo);
        }

        public abstract object Deserialize(in DeserializeInfo info, ref BitSource header);
    }
}