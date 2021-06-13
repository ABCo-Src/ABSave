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
    public abstract class Converter<T> : MapItem
    {
        internal SaveInheritanceAttribute[]? _allInheritanceAttributes = null;
        internal Dictionary<uint, SaveInheritanceAttribute?>? _inheritanceValues = null;

        /// <summary>
        /// Whether this type converter can also convert things other than exact types. If this is enabled, ABSave will also call <see cref="TryGenerateContext(ABSaveSettings, Type)"/>, and if it generates a context this converter will be used.
        /// </summary>
        public abstract bool AlsoConvertsNonExact { get; }

        /// <summary>
        /// All the exact types of data this converter can convert.
        /// </summary>
        public virtual Type[] ExactTypes { get; } = Array.Empty<Type>();

        public abstract void Initialize(ref ContextGen gen);

        /// <summary>
        /// Attempts to generate a context.
        /// </summary>
        public abstract void TryGenerateContext(ref ContextGen gen);

        public virtual bool UsesHeaderForVersion(uint version) => false;
        public abstract void Serialize(object obj, Type actualType, ref BitTarget header);
        public abstract object Deserialize(Type actualType, ref BitSource header);
    }
}