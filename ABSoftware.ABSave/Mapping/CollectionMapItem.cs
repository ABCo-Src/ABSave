using ABSoftware.ABSave.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Mapping
{
    public class CollectionMapItem : ABSaveMapItem
    {
        public ABSaveMapItem PerItem;
        public CollectionInfo Info;

        public Type ElementType;
        public bool AreElementsSameType;

        public CollectionMapItem(bool canBeNull, Type elementType, CollectionInfo info, ABSaveMapItem perItem) : base(canBeNull)
        {
            Info = info;
            ElementType = elementType;
            PerItem = perItem;
        }

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
