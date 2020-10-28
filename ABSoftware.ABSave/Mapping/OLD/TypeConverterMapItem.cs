using ABSoftware.ABSave.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Mapping
{
    public class TypeConverterMapItem : ABSaveMapItemOLD
    {
        public readonly ABSaveTypeConverter TypeConverter;
        public TypeConverterMapItem(bool canBeNull, ABSaveTypeConverter typeConverter) : base(canBeNull) => TypeConverter = typeConverter;

        protected override void DoSerialize(object obj, Type specifiedType, ABSaveWriter writer) => TypeConverter.Serialize(obj, specifiedType, writer);
        protected override object DoDeserialize(Type specifiedType, ABSaveReader reader) => TypeConverter.Deserialize(specifiedType, reader);
    }
}
