using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Mapping.Items
{
    /// <summary>
    /// A map item determined at runtime
    /// </summary>
    internal class RuntimeMapItem : MapItem
    {
        public MapItem InnerItem;

        public RuntimeMapItem(MapItem innerItem) =>
            (InnerItem, ItemType) = (innerItem, innerItem.ItemType);
    }
}
