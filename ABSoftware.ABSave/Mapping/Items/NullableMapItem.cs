using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Mapping.Items
{
    public class NullableMapItem : MapItem
    {
        public MapItem InnerItem;

        public NullableMapItem(Type itemType, MapItem innerItem) =>
            (ItemType, InnerItem) = (itemType, innerItem);
    }
}
