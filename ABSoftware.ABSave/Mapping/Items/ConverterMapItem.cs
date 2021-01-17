using ABSoftware.ABSave.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Mapping.Items
{
    public class ConverterMapItem : MapItem
    {
        public ABSaveConverter Converter { get; }
        public IABSaveConverterContext Context { get; }

        public ConverterMapItem(Type itemType, ABSaveConverter converter, IABSaveConverterContext context) =>
            (ItemType, Converter, Context) = (itemType, converter, context);
    }
}
