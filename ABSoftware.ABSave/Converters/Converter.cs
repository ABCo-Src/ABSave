using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Mapping.Generation;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave.Converters
{
    /// <summary>
    /// Represents the information gathered about a type that's necessary for conversion.
    /// </summary>
    public interface IConverterContext { }

    public abstract class Converter
    {
        /// <summary>
        /// Whether this converter can be used for sub-classes of the class it was originally selected for.
        /// </summary>
        public abstract bool ConvertsSubTypes { get; }

        /// <summary>
        /// Whether the converter wants to store data in the extra 6 or 7-bits of header data ABSave has available on the item.
        /// </summary>
        public virtual bool WritesToHeader => false;

        /// <summary>
        /// Whether this type converter can also convert things other than exact types. If this is enabled, ABSave will also call <see cref="TryGenerateContext(ABSaveSettings, Type)"/>, and if it generates a context this converter will be used.
        /// </summary>
        public abstract bool AlsoConvertsNonExact { get; }

        /// <summary>
        /// All the exact types of data this converter can convert.
        /// </summary>
        public virtual Type[] ExactTypes { get; } = Array.Empty<Type>();

        /// <summary>
        /// Attempts to generate a context. If the converter has non-exact types, this should return null when this converter doesn't convert the given type.
        /// </summary>
        public abstract IConverterContext? TryGenerateContext(ref ContextGen gen);

        public abstract void Serialize(object obj, Type actualType, IConverterContext context, ref BitTarget header);
        public abstract object Deserialize(Type actualType, IConverterContext context, ref BitSource header);

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