using ABSoftware.ABSave.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Mapping
{
    public class DictionaryMapItem : ABSaveMapItem
    {
        public ABSaveDictionaryInfo Info;

        public Type KeyType;
        public ABSaveMapItem PerKey;

        public Type ValueType;
        public ABSaveMapItem PerValue;

        public DictionaryMapItem(bool canBeNull, ABSaveDictionaryInfo info, Type keyType, ABSaveMapItem perKey, Type valueType, ABSaveMapItem perValue) : base(canBeNull)
        {
            Info = info;

            KeyType = keyType;
            PerKey = perKey;

            ValueType = valueType;
            PerValue = perValue;
        }

        protected override void DoSerialize(object obj, Type specifiedType, ABSaveWriter writer) => EnumerableTypeConverter.Instance.SerializeDictionaryMap(specifiedType, writer, this);
        protected override object DoDeserialize(Type specifiedType, ABSaveReader reader) => EnumerableTypeConverter.Instance.DeserializeDictionaryMap(specifiedType, reader, this);
    }
}
