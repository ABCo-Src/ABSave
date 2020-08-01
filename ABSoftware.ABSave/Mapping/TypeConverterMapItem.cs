using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization.Writer;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Mapping
{
    public class TypeConverterMapItem : ABSaveMapItem
    {
        public readonly ABSaveTypeConverter TypeConverter;
        public TypeConverterMapItem(ABSaveTypeConverter typeConverter) => TypeConverter = typeConverter;

        public override void Serialize(object obj, TypeInformation typeInfo, ABSaveWriter writer) => TypeConverter.Serialize(obj, typeInfo, writer);
    }
}
