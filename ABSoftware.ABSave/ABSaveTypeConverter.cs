using ABSoftware.ABSave.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave
{
    public abstract class ABSaveTypeConverter
    {
        /// <summary>
        /// Whether this type converter can also convert things other than exact types.
        /// </summary>
        public abstract bool HasNonExactTypes { get; }

        /// <summary>
        /// All the exact types of data this converter can convert.
        /// </summary>
        public virtual Type[] ExactTypes { get; } = new Type[0];

        /// <summary>
        /// Whether this converter has custom data to write to the map.
        /// </summary>
        public virtual bool HasCustomMapData { get; } = false;

        /// <summary>
        /// Attempts to generate a context. If the converter has non-exact types, this will be used to determine if the converter is available too.
        /// </summary>
        public abstract IABSaveConverterContext TryGenerateContext(ABSaveSettings settings, Type type);
        public abstract void SerializeData(object obj, Type actualType, IABSaveConverterContext context, ABSaveWriter writer);
        public abstract object DeserializeData(Type type, IABSaveConverterContext context, ABSaveReader reader);
        public virtual void SerializeMap(object obj, Type actualType, IABSaveConverterContext context, ABSaveWriter writer) { }

        internal static readonly Dictionary<Type, ABSaveTypeConverter> BuiltInExact = new Dictionary<Type, ABSaveTypeConverter>()
        {
            { typeof(byte), NumberTypeConverter.Instance },
            { typeof(sbyte), NumberTypeConverter.Instance },
            { typeof(char), NumberTypeConverter.Instance },
            { typeof(ushort), NumberTypeConverter.Instance },
            { typeof(short), NumberTypeConverter.Instance },
            { typeof(uint), NumberTypeConverter.Instance },
            { typeof(int), NumberTypeConverter.Instance },
            { typeof(ulong), NumberTypeConverter.Instance },
            { typeof(long), NumberTypeConverter.Instance },
            { typeof(float), NumberTypeConverter.Instance },
            { typeof(double), NumberTypeConverter.Instance },
            { typeof(decimal), NumberTypeConverter.Instance },
            { typeof(bool), BooleanTypeConverter.Instance },
            { typeof(Guid), GuidTypeConverter.Instance },
            { typeof(StringBuilder), StringBuilderTypeConverter.Instance },
            { typeof(string), StringTypeConverter.Instance },
            { typeof(Version), VersionTypeConverter.Instance },
            { typeof(DateTime), DateTimeTypeConverter.Instance },
            { typeof(TimeSpan), TimeSpanTypeConverter.Instance },
            { typeof(DictionaryEntry), DictionaryEntryConverter.Instance },
            { TypeTypeConverter.RuntimeType, TypeTypeConverter.Instance },
            { AssemblyTypeConverter.RuntimeAssembly, AssemblyTypeConverter.Instance },

            // Supposedly the most common array types, remember to update in "ArrayTypeConverter" too if changing.
            { typeof(Array), ArrayTypeConverter.Instance },
            { typeof(string[]), ArrayTypeConverter.Instance },
            { typeof(int[]), ArrayTypeConverter.Instance },
        };

        internal static List<ABSaveTypeConverter> BuiltInNonExact = new List<ABSaveTypeConverter>()
        {
            KeyValueConverter.Instance,
            AssemblyTypeConverter.Instance,
            TypeTypeConverter.Instance,
            EnumerableTypeConverter.Instance,
            ArrayTypeConverter.Instance,
        };
    }

   
}
