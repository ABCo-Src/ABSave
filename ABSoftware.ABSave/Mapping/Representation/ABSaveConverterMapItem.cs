using ABSoftware.ABSave.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Mapping.Representation
{
    public class ABSaveConverterMapItem : ABSaveMapItem
    {
        public ABSaveTypeConverter Converter;
        public Type ConverterType;

        public ABSaveConverterMapItem(ABSaveTypeConverter converter)
        {
            Converter = converter;
            ConverterType = converter.GetType();
        }

        public override void SerializeData(object obj, Type type, ABSaveWriter writer)
        {
            ABSaveItemConverter.SerializeAttribute(obj, obj.GetType(), type, writer);
            Converter.Serialize(obj, type, writer);
        }
    }
}
