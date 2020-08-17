using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Serialization;
using System;

namespace ABSoftware.ABSave.Converters
{
    public class NumberTypeConverter : ABSaveTypeConverter
    {
        // Enums get the TypeCode of Int32.
        public static NumberTypeConverter Instance = new NumberTypeConverter();
        private NumberTypeConverter() { }

        public override bool HasExactType => false;
        public override bool CheckCanConvertType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Char:
                case TypeCode.UInt16:
                case TypeCode.Int16:
                case TypeCode.UInt32:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;
                default:
                    return false;
            }
        }

        public override void Serialize(object obj, Type type, ABSaveWriter writer) => writer.WriteNumber(obj, Type.GetTypeCode(type));
        public override object Deserialize(Type type, ABSaveReader reader) => reader.ReadNumber(Type.GetTypeCode(type));
    }
}
