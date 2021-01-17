using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Mapping.Items
{
    public class NullableMapItem : MapItem
    {
        public MapItem InnerItem;

        public NullableMapItem(Type itemType, bool isValueType, MapItem innerItem) =>
            (ItemType, IsValueType, InnerItem) = (itemType, isValueType, innerItem);
    }
}
