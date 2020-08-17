using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Mapping
{
    public class CollectionMapItem : ABSaveMapItem
    {
        public Func<ICollectionWrapper> CreateWrapper;
        public ABSaveMapItem PerItem;

        public bool IsArray;
        public Type ArrayElementType;
        public bool ElementsSameType;

        public static CollectionMapItem ForArray(bool canBeNull, Type elementType, bool elementsSameType, ABSaveMapItem perItem) => new CollectionMapItem(canBeNull, elementsSameType)
        {
            ArrayElementType = elementType,
            PerItem = perItem,
            IsArray = true
        };

        public static CollectionMapItem ForICollection(bool canBeNull, Func<ICollectionWrapper> createWrapper, ABSaveMapItem perItem) => new CollectionMapItem(canBeNull, elementsSameType)
        {
            CreateWrapper = createWrapper,
            PerItem = perItem
        };

        private CollectionMapItem(bool canBeNull, bool elementsSameType) : base(canBeNull) => ElementsSameType = elementsSameType;

        public override void Serialize(object obj, Type type, ABSaveWriter writer)
        {
            if (SerializeNullAttribute(obj, writer)) return;
            CollectionTypeConverter.Instance.Serialize(obj, writer, this);
        }
        public override object Deserialize(Type type, ABSaveReader reader)
        {
            if (DeserializeNullAttribute(reader)) return null;
            return CollectionTypeConverter.Instance.Deserialize(type, reader, this);
        }
    }
}
