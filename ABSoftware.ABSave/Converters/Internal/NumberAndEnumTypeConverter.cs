using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave.Converters.Internal
{
    public class NumberAndEnumTypeConverter : ABSaveTypeConverter
    {
        // Enums get the TypeCode of Int32.
        public static NumberAndEnumTypeConverter Instance = new NumberAndEnumTypeConverter();
        private NumberAndEnumTypeConverter() { }

        public override bool HasExactType => false;
        public override bool CheckCanConvertType(TypeInformation typeInformation)
        {
            switch (typeInformation.ActualTypeCode)
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

        public override void Serialize(object obj, TypeInformation typeInfo, ABSaveWriter writer)
        {
            writer.WriteNumber(obj, typeInfo.ActualTypeCode);
        }
    }
}
