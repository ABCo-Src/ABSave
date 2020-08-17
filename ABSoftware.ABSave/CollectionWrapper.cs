using ABSoftware.ABSave.Mapping;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave
{
    /// <summary>
    /// Represents a wrapper around an unknown "ICollection". This is used to make many types of collections (generic, non-generic, read-only etc.) work the same.
    /// </summary>
    public interface ICollectionWrapper
    {
        // TO CONSIDER: Use IList indexers for faster performance? Currently just using enumerators for everything.
        Type ElementType { get; }
        int Count { get; }
        void AddItem(object item);
        IEnumerator GetEnumerator();
        void SetCollection(object collection);
        object CreateCollection(int capacity, Type collectionType);
    }

    public class GenericICollectionWrapper<T> : ICollectionWrapper
    {
        ICollection<T> BaseCollection;

        public Type ElementType => typeof(T);
        public int Count => BaseCollection.Count;
        public IEnumerator GetEnumerator() => BaseCollection.GetEnumerator();
        public void AddItem(object item) => BaseCollection.Add((T)item);
        public void SetCollection(object collection) => BaseCollection = (ICollection<T>)collection;
        public object CreateCollection(int capacity, Type collectionType) => BaseCollection = (ICollection<T>)Activator.CreateInstance(collectionType);
    }

    public class NonGenericIListWrapper : ICollectionWrapper
    {
        IList BaseCollection;

        public Type ElementType => typeof(object);
        public int Count => BaseCollection.Count;
        public IEnumerator GetEnumerator() => BaseCollection.GetEnumerator();
        public void AddItem(object item) => BaseCollection.Add(item);
        public void SetCollection(object collection) => BaseCollection = (IList)collection;
        public object CreateCollection(int capacity, Type collectionType) => BaseCollection = (IList)Activator.CreateInstance(collectionType);
    }
}
