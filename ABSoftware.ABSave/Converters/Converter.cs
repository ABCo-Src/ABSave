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
using System.Runtime.InteropServices;
using System.Text;

namespace ABCo.ABSave.Converters
{
    public abstract class Converter : MapItem
    {
        internal SaveInheritanceAttribute[]? _allInheritanceAttributes = null;
        internal Dictionary<uint, SaveInheritanceAttribute?>? _inheritanceValues = null;

        internal ConverterVersionCache VersionCache;

        [StructLayout(LayoutKind.Explicit)]
        internal struct ConverterVersionCache
        {
            [FieldOffset(0)]
            public VersionInfo OneVersion;

            [FieldOffset(0)]
            public Dictionary<uint, VersionInfo?> MultipleVersions;
        }

        /// <summary>
        /// Initializes a given converter for a given type.
        /// </summary>
        public virtual void Initialize(InitializeInfo info) { }

        /// <summary>
        /// Check whether the converter supports a given type, used for non-exact types.
        /// This method is allowed to modify variables, however it is nt.
        /// </summary>
        public virtual bool CheckType(CheckTypeInfo info) => throw new Exception("Converter says it also converts non-exact but does not override 'CheckType' to check for one.");

        /// <summary>
        /// Gets information that can be used by the converter and varies depending on the version number in the source.
        /// This info will be cached and may be used across many threads so ensure it does not change once created.
        /// </summary>
        public virtual (VersionInfo?, bool) GetVersionInfo(uint version) => (null, false);

        /// <summary>
        /// Called when all of the different possible versions right up to the highest version have been generated.
        /// Can be used to free resources that aren't needed if all versions are generated.
        /// </summary>
        public virtual void HandleAllVersionsGenerated() { }

        public struct SerializeInfo
        {
            public object Instance { get; }
            public Type ActualType { get; }
            public VersionInfo VersionInfo { get; }

            internal SerializeInfo(object instance, Type actualType, VersionInfo versionInfo) => 
                (Instance, ActualType, VersionInfo) = (instance, actualType, versionInfo);
        }

        public abstract void Serialize(in SerializeInfo info, ref BitTarget header);

        public struct DeserializeInfo
        {
            public Type ActualType { get; }
            internal ConverterVersionInfo VersionInfo { get; }

            internal DeserializeInfo(Type actualType, ConverterVersionInfo versionInfo) => 
                (ActualType, VersionInfo) = (actualType, versionInfo);
        }

        public abstract object Deserialize(in DeserializeInfo info, ref BitSource header);
    }
}