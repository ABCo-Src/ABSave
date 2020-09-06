using ABSoftware.ABSave.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Mapping
{
    public class ArrayMapItem : ABSaveMapItem
    {
        public Type ElementType;
        public bool AreElementsSameType;

        public ABSaveMapItem PerItem;

        public ArrayMapItem(bool canBeNull, Type elementType, bool elementSameType, ABSaveMapItem perItem) : base(canBeNull)
        {
            ElementType = elementType;
            AreElementsSameType = elementSameType;
            PerItem = perItem;
        }

        public override void Serialize(object obj, Type type, ABSaveWriter writer)
        {
            if (SerializeNullAttribute(obj, writer)) return;
            ArrayTypeConverter.Instance.Serialize((Array)obj, writer, this);
        }

        public override object Deserialize(Type type, ABSaveReader reader)
        {
            if (DeserializeNullAttribute(reader)) return null;
            return ArrayTypeConverter.Instance.Deserialize(reader, this);
        }
    }
}
