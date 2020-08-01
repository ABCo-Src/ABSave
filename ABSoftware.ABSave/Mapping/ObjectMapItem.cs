using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization.Writer;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Mapping
{
    public class ObjectMapItem : ABSaveMapItem
    {
        private int _itemsAdded = 0;
        public int NumberOfItems;

        internal bool Initialized;
        internal ABSaveMapItem[] Items;

        public ObjectMapItem(int numberOfItems)
        {
            NumberOfItems = numberOfItems;
            Items = new ABSaveMapItem[numberOfItems];
        }

        public ObjectMapItem AddItem(string name, ABSaveMapItem mapItem)
        {
            if (_itemsAdded == NumberOfItems) throw new Exception("ABSAVE: Too many items added to an object map, make sure to set the correct size in the constructor.");

            mapItem.Name = name;
            Items[_itemsAdded++] = mapItem;

            return this;
        }

        public ObjectMapItem AddItem<TObject, TItem>(string name, Func<TObject, TItem> getter, Action<TObject, TItem> setter, ABSaveMapItem mapItem)
        {
            mapItem.UseReflection = false;
            mapItem.Getter = container => getter((TObject)container);
            mapItem.Setter = (container, itm) => setter((TObject)container, (TItem)itm);
            mapItem.FieldType = typeof(TItem);
            return AddItem(name, mapItem);
        }

        public override void Serialize(object obj, TypeInformation typeInfo, ABSaveWriter writer) => ABSaveObjectConverter.Serialize(obj, typeInfo, writer, this);
    }
}
