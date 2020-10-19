using ABSoftware.ABSave.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Mapping
{
    public class ArrayMapItem : ABSaveMapItem
    {
        public Type ElementType;
        public ABSaveMapItem PerItem;

        public ArrayMapItem(bool canBeNull, Type elementType, ABSaveMapItem perItem) : base(canBeNull)
        {
            ElementType = elementType;
            PerItem = perItem;
        }

        protected override void DoSerialize(object obj, Type specifiedType, ABSaveWriter writer) => ArrayTypeConverter.Instance.Serialize((Array)obj, writer, this);
        protected override object DoDeserialize(Type specifiedType, ABSaveReader reader) => ArrayTypeConverter.Instance.Deserialize(reader, this);
    }
}
