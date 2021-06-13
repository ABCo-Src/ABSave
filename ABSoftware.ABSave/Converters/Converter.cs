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
        /// Whether this type converter can also convert things other than exact types. If this is enabled, ABSave will also call <see cref="TryGenerateContext(ABSaveSettings, Type)"/>, and if it generates a context this converter will be used.
        /// </summary>
        public abstract bool AlsoConvertsNonExact { get; }

        /// <summary>
        /// All the exact types of data this converter can convert.
        /// </summary>
        public virtual Type[] ExactTypes { get; } = Array.Empty<Type>();

        /// <summary>
        /// Attempts to generate a context.
        /// </summary>
        public abstract void TryGenerateContext(ref ContextGen gen);

        public virtual bool UsesHeaderForVersion(uint version) => false;
        public abstract void Serialize(object obj, Type actualType, ref BitTarget header);
        public abstract object Deserialize(Type actualType, ref BitSource header);

        internal static readonly IReadOnlyDictionary<Type, Converter> BuiltInExact = new Dictionary<Type, Converter>()
        {
            { typeof(byte), PrimitiveConverter.Instance },
            { typeof(sbyte), PrimitiveConverter.Instance },
            { typeof(char), PrimitiveConverter.Instance },
            { typeof(ushort), PrimitiveConverter.Instance },
            { typeof(short), PrimitiveConverter.Instance },
            { typeof(uint), PrimitiveConverter.Instance },
            { typeof(int), PrimitiveConverter.Instance },
            { typeof(ulong), PrimitiveConverter.Instance },
            { typeof(long), PrimitiveConverter.Instance },
            { typeof(float), PrimitiveConverter.Instance },
            { typeof(double), PrimitiveConverter.Instance },
            { typeof(decimal), PrimitiveConverter.Instance },
            { typeof(IntPtr), PrimitiveConverter.Instance },
            { typeof(UIntPtr), PrimitiveConverter.Instance },
            { typeof(bool), PrimitiveConverter.Instance },
            { typeof(Guid), GuidConverter.Instance },
            { typeof(string), TextConverter.Instance },
            { typeof(StringBuilder), TextConverter.Instance },
            { typeof(char[]), TextConverter.Instance },
            { typeof(Version), VersionConverter.Instance },
            { typeof(DateTime), TickBasedConverter.Instance },
            { typeof(TimeSpan), TickBasedConverter.Instance },
            { typeof(DictionaryEntry), KeyValueConverter.Instance },
            { typeof(Type), TypeConverter.Instance },
            { typeof(Assembly), AssemblyConverter.Instance },
            { typeof(IEnumerable), EnumerableConverter.Instance },

            // Based on the most common array types in .NET, remember to update in "ArrayTypeConverter" too if changing.
            { typeof(Array), ArrayConverter.Instance },
            { typeof(byte[]), ArrayConverter.Instance },
            { typeof(string[]), ArrayConverter.Instance },
            { typeof(int[]), ArrayConverter.Instance },
        };

        internal static IReadOnlyList<Converter> BuiltInNonExact = new List<Converter>()
        {
            KeyValueConverter.Instance,
            AssemblyConverter.Instance,
            TypeConverter.Instance,
            EnumerableConverter.Instance,
            ArrayConverter.Instance,
            PrimitiveConverter.Instance,
        };
    }
}