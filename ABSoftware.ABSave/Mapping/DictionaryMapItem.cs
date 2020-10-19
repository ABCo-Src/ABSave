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
        public bool AreKeysSameType;

        public Type ValueType;
        public ABSaveMapItem PerValue;
        public bool AreValuesSameType;

        public DictionaryMapItem(bool canBeNull, ABSaveDictionaryInfo info, Type keyType, ABSaveMapItem perKey, bool areKeysSameType, Type valueType, ABSaveMapItem perValue, bool areValuesSameType) : base(canBeNull)
        {
            Info = info;

            KeyType = keyType;
            PerKey = perKey;
            AreKeysSameType = areKeysSameType;

            ValueType = valueType;
            PerValue = perValue;
            AreValuesSameType = areValuesSameType;
        }

        protected override void DoSerialize(object obj, Type specifiedType, ABSaveWriter writer) => EnumerableTypeConverter.Instance.Serialize(obj, writer, this);
        protected override object DoDeserialize(Type specifiedType, ABSaveReader reader) => EnumerableTypeConverter.Instance.Deserialize(specifiedType, reader);
    }
}
