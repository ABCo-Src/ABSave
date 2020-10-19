using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Converters
{
    public abstract class ABSaveTypeConverter
    {
        /// <summary>
        /// Whether this type converter only converts one exact type.
        /// If it does, that type should be given using <see cref="ExactType"/>.
        /// If it doesn't (e.g. this converter converts lots of different types), then the type should be checked with <see cref="CheckCanConvertType(Type)"/>.
        /// </summary>
        public abstract bool HasExactType { get; }
        public virtual Type ExactType => null;

        /// <summary>
        /// Manually checks whether this converter converts the given type. Should only be used if this type converter doesn't convert exact types.
        /// </summary>
        public virtual bool CheckCanConvertType(Type type) => throw new NotImplementedException("ABSAVE: This type converter hasn't implemented 'CheckCanConvertType'");
        public abstract void Serialize(object obj, Type type, ABSaveWriter writer);
        public abstract object Deserialize(Type type, ABSaveReader reader);

        internal static Dictionary<Type, ABSaveTypeConverter> BuiltInExact = new Dictionary<Type, ABSaveTypeConverter>()
        {
            { typeof(bool), BooleanTypeConverter.Instance },
            { typeof(Guid), GuidTypeConverter.Instance },
            { typeof(StringBuilder), StringBuilderTypeConverter.Instance },
            { typeof(string), StringTypeConverter.Instance },
            { typeof(Version), VersionTypeConverter.Instance },
            { typeof(DateTime), DateTimeTypeConverter.Instance },
            { typeof(TimeSpan), TimeSpanTypeConverter.Instance },
            { typeof(DictionaryEntry), DictionaryEntryConverter.Instance }
        };

        internal static List<ABSaveTypeConverter> BuiltInNonExact = new List<ABSaveTypeConverter>()
        {
            AssemblyTypeConverter.Instance,
            TypeTypeConverter.Instance,
            KeyValueConverter.Instance,
            EnumerableTypeConverter.Instance,
            ArrayTypeConverter.Instance,
            NumberTypeConverter.Instance
        };
    }
}
