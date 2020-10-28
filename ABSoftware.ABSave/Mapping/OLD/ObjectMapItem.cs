﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Mapping
{
    public class ObjectMapItem : ABSaveMapItemOLD
    {
        private int _itemsAdded = 0;
        public int NumberOfItems;
        public Func<object> Constructor;

        internal bool Initialized;
        internal ABSaveMapItemOLD[] Items;
        internal Dictionary<string, ABSaveMapItemOLD> HashedItems;

        public ObjectMapItem(bool canBeNull, Func<object> constructor, int numberOfItems) : base(canBeNull)
        {
            NumberOfItems = numberOfItems;
            Constructor = constructor;
            Items = new ABSaveMapItemOLD[numberOfItems];
            HashedItems = new Dictionary<string, ABSaveMapItemOLD>(numberOfItems);
        }

        public ObjectMapItem AddItem(string name, ABSaveMapItemOLD mapItem)
        {
            if (_itemsAdded == NumberOfItems) throw new Exception("ABSAVE: Too many items added to an object map, make sure to set the correct size in the constructor.");

            mapItem.Name = name;
            Items[_itemsAdded++] = mapItem;
            HashedItems.Add(name, mapItem);

            return this;
        }

        public ObjectMapItem AddItem<TObject, TItem>(string name, Func<TObject, TItem> getter, Action<TObject, TItem> setter, ABSaveMapItemOLD mapItem)
        {
            mapItem.UseReflection = false;
            mapItem.Getter = container => getter((TObject)container);
            mapItem.Setter = (container, itm) => setter((TObject)container, (TItem)itm);
            mapItem.FieldType = typeof(TItem);
            return AddItem(name, mapItem);
        }

        protected override void DoSerialize(object obj, Type specifiedType, ABSaveWriter writer) => ABSaveObjectConverter.Serialize(obj, specifiedType, writer, this);
        protected override object DoDeserialize(Type specifiedType, ABSaveReader reader) => ABSaveObjectConverter.Deserialize(specifiedType, reader, this);
    }

    internal class ObjectSubMapItemCustom
    {
        
    }

    internal class ObjectSubMapItem
    {
        internal ABSaveMapItemOLD BaseItem;
        internal string Name;
        internal bool UseReflection = true;
        internal Func<object, object> Getter = null;
        internal Action<object, object> Setter = null;
        internal Type FieldType = null;
        internal bool CanBeNull = false;
    }
}