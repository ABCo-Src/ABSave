using ABSoftware.ABSave.Mapping;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave
{
    /// <summary>
    /// Represents a standard way to do things for different types of collections.
    /// </summary>
    public abstract class CollectionInfo
    {
        public static CollectionInfo GenericICollection { get; } = new GenericICollectionInfo();
        public static CollectionInfo NonGenericIList { get; } = new NonGenericIListInfo();
        public static CollectionInfo GenericIDictionary { get; } = new GenericIDictionaryInfo();
        public static CollectionInfo NonGenericIDictionary { get; } = new NonGenericIDictionaryInfo();

        public virtual int GetCount(object obj) => ((dynamic)obj).Count;
        public virtual void AddItem(object obj, object itm) => ((dynamic)obj).Add(itm);
        public virtual object CreateCollection(Type type, int count) => (dynamic)Activator.CreateInstance(type);
    }

    internal class GenericICollectionInfo : CollectionInfo { }
    internal class NonGenericIListInfo : CollectionInfo
    {
        public override int GetCount(object obj) => ((IList)obj).Count;
        public override void AddItem(object obj, object itm) => ((IList)obj).Add(itm);
        public override object CreateCollection(Type type, int count) => (IList)Activator.CreateInstance(type);
    }

    internal class GenericIDictionaryInfo : CollectionInfo
    {
        public override void AddItem(object obj, object itm)
        {
            var keyValuePair = (dynamic)itm;
            ((dynamic)obj).Add(keyValuePair.Key, keyValuePair.Value);
        }
    }

    internal class NonGenericIDictionaryInfo : CollectionInfo
    {
        public override void AddItem(object obj, object itm)
        {
            var keyValuePair = (DictionaryEntry)itm;
            ((IDictionary)obj).Add(keyValuePair.Key, keyValuePair.Value);
        }

        public override object CreateCollection(Type type, int count) => (IDictionary)Activator.CreateInstance(type);
    }
}
