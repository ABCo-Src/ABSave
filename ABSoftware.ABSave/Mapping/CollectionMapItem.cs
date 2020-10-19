using ABSoftware.ABSave.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Mapping
{
    public class CollectionMapItem : ABSaveMapItem
    {
        public ABSaveMapItem PerItem;
        public ABSaveCollectionInfo Info;

        public Type ElementType;

        public CollectionMapItem(bool canBeNull, Type elementType, ABSaveCollectionInfo info, ABSaveMapItem perItem) : base(canBeNull)
        {
            Info = info;
            ElementType = elementType;
            PerItem = perItem;
        }

        protected override void DoSerialize(object obj, Type specifiedType, ABSaveWriter writer) => EnumerableTypeConverter.Instance.SerializeCollectionMap(obj, writer, this);
        protected override object DoDeserialize(Type specifiedType, ABSaveReader reader) => EnumerableTypeConverter.Instance.DeserializeCollectionMap(specifiedType, reader, this);
    }
}
