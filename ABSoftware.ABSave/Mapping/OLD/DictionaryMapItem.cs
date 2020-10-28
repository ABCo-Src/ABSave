using ABSoftware.ABSave.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Mapping
{
    public class DictionaryMapItem : ABSaveMapItemOLD
    {
        public ABSaveDictionaryInfo Info;

        public Type KeyType;
        public ABSaveMapItemOLD PerKey;

        public Type ValueType;
        public ABSaveMapItemOLD PerValue;

        public DictionaryMapItem(bool canBeNull, ABSaveDictionaryInfo info, Type keyType, ABSaveMapItemOLD perKey, Type valueType, ABSaveMapItemOLD perValue) : base(canBeNull)
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
