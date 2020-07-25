using ABSoftware.ABSave.Converters.Internal;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave.Converters
{
    public abstract class ABSaveTypeConverter
    {
        /// <summary>
        /// Whether this type converter only converts one exact type.
        /// If it does, that type should be given using <see cref="ExactType"/>.
        /// If it doesn't (e.g. this converter converts lots of different types), then the type should be checked with <see cref="CheckCanConvertType(TypeInformation)"/>.
        /// </summary>
        public abstract bool HasExactType { get; }
        public virtual Type ExactType => null;

        /// <summary>
        /// Manually checks whether this converter converts the given type. Should only be used if this type converter doesn't convert exact types.
        /// </summary>
        public virtual bool CheckCanConvertType(TypeInformation typeInformation) => false;
        public abstract void Serialize(object obj, TypeInformation typeInfo, ABSaveWriter writer);

        #region Type Converter Management

        internal static Dictionary<Type, ABSaveTypeConverter> BuiltInExact = new Dictionary<Type, ABSaveTypeConverter>()
        {
            { typeof(Assembly), AssemblyTypeConverter.Instance },
            { typeof(bool), BooleanTypeConverter.Instance },
            { typeof(Guid), GuidTypeConverter.Instance },
            { typeof(StringBuilder), StringBuilderTypeConverter.Instance },
            { typeof(string), StringTypeConverter.Instance },
            { typeof(Type), TypeTypeConverter.Instance },
            { typeof(Version), VersionTypeConverter.Instance },
            { typeof(DateTime), DateTimeTypeConverter.Instance }
        };

        internal static List<ABSaveTypeConverter> BuiltInNonExact = new List<ABSaveTypeConverter>()
        {
            NumberAndEnumTypeConverter.Instance
        };

        #endregion
    }
}
