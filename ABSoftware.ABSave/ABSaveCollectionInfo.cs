using ABSoftware.ABSave.Exceptions;
using ABSoftware.ABSave.Mapping;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ABSoftware.ABSave
{
    public interface IABSaveEnumerableInfo { }

    // Collections:
    /// <summary>
    /// Represents a standard way to do things for different types of collections.
    /// </summary>
    public abstract class ABSaveCollectionInfo : IABSaveEnumerableInfo
    {
        public static ABSaveCollectionInfo GenericICollection { get; } = new GenericICollectionInfo();
        public static ABSaveCollectionInfo NonGenericIList { get; } = new NonGenericIListInfo();
        public static ABSaveCollectionInfo List { get; } = new ListInfo();

        public abstract int GetCount(object obj);
        public abstract IEnumerator GetEnumerator(object obj);
        public abstract object CreateCollection(Type type, int count);
        public abstract void AddItem(object obj, object itm);
    }

    // Regular Collections:
    internal class ListInfo : ABSaveCollectionInfo
    {
        public override int GetCount(object obj) => ((ICollection)obj).Count;
        public override IEnumerator GetEnumerator(object obj) => ((IEnumerable)obj).GetEnumerator();
        public override object CreateCollection(Type type, int count) => Activator.CreateInstance(type, count);
        public override void AddItem(object obj, object itm) => ((IList)obj).Add(itm);
    }

    internal class GenericICollectionInfo : ABSaveCollectionInfo
    {
        public override int GetCount(object obj) => ((dynamic)obj).Count;
        public override IEnumerator GetEnumerator(object obj) => ((IEnumerable)obj).GetEnumerator();
        public override object CreateCollection(Type type, int count) => (dynamic)Activator.CreateInstance(type);
        public override void AddItem(object obj, object itm) => ((dynamic)obj).Add(itm);
    }

    internal class NonGenericIListInfo : ABSaveCollectionInfo
    {
        public override int GetCount(object obj) => ((IList)obj).Count;
        public override IEnumerator GetEnumerator(object obj) => ((IEnumerable)obj).GetEnumerator();
        public override object CreateCollection(Type type, int count) => (IList)Activator.CreateInstance(type);
        public override void AddItem(object obj, object itm) => ((IList)obj).Add(itm);
    }

    // Dictionaries:
    public abstract class ABSaveDictionaryInfo : IABSaveEnumerableInfo
    {
        public static ABSaveDictionaryInfo GenericIDictionary { get; } = new GenericIDictionaryInfo();
        public static ABSaveDictionaryInfo NonGenericIDictionary { get; } = new NonGenericIDictionaryInfo();

        public abstract int GetCount(object obj);
        public abstract IDictionaryEnumerator GetEnumerator(object obj);
        public abstract object CreateCollection(Type type, int count);
        public abstract void AddItem(object obj, object key, object value);
    }

    internal class GenericIDictionaryInfo : ABSaveDictionaryInfo
    {
        public override int GetCount(object obj) => ((dynamic)obj).Count;
        public override IDictionaryEnumerator GetEnumerator(object obj)
        {
            if (((dynamic)obj).GetEnumerator() is IDictionaryEnumerator asDictEnumerator) return asDictEnumerator;
            else throw new ABSaveInvalidDictionaryException(obj.GetType());
        }

        public override object CreateCollection(Type type, int count) => (dynamic)Activator.CreateInstance(type);
        public override void AddItem(object obj, object key, object value) => ((dynamic)obj).Add(key, value);
    }

    internal class NonGenericIDictionaryInfo : ABSaveDictionaryInfo
    {
        public override int GetCount(object obj) => ((IDictionary)obj).Count;
        public override IDictionaryEnumerator GetEnumerator(object obj) => ((IDictionary)obj).GetEnumerator();
        public override object CreateCollection(Type type, int count) => (IDictionary)Activator.CreateInstance(type);
        public override void AddItem(object obj, object key, object value) => ((IDictionary)obj).Add(key, value);
    }
}
