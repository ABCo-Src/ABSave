using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Mapping
{
    public class TypeConverterMapItem : ABSaveMapItem
    {
        public readonly ABSaveTypeConverter TypeConverter;
        public TypeConverterMapItem(bool canBeNull, ABSaveTypeConverter typeConverter) : base(canBeNull) => TypeConverter = typeConverter;

        public override void Serialize(object obj, Type type, ABSaveWriter writer) => TypeConverter.Serialize(obj, type, writer);
        public override object Deserialize(Type type, ABSaveReader reader) => TypeConverter.Deserialize(type, reader);
    }
}
