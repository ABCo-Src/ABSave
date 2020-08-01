using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization.Writer;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Mapping
{
    public class CollectionMapItem : ABSaveMapItem
    {
        public CollectionType CollectionType;
        public Type ItemType;
        public ABSaveMapItem ItemConverter;

        public CollectionMapItem(CollectionType collectionType, Type itemType, ABSaveMapItem itemConverter)
        {
            CollectionType = collectionType;
            ItemType = itemType;
            ItemConverter = itemConverter;
        }

        public override void Serialize(object obj, TypeInformation typeInfo, ABSaveWriter writer) => CollectionTypeConverter.Instance.Serialize(obj, typeInfo, writer);
    }
}
