using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave
{
    /// <summary>
    /// Represents a wrapper around an unknown "ICollection". This is used to make all types of collections (generic vs non-generic, IList vs ICollection etc.) work the same.
    /// </summary>
    internal interface ICollectionWrapper
    {
        Type ElementType { get; }
        void AddItem(object item);
        void SetCollection(object collection);
    }

    internal interface ICollectionWrapperIndex
    {
        object GetAtIndex(int index);
    }

    internal interface ICollectionWrapperEnumerator
    {
        IEnumerator GetEnumerator();
    }

    public class GenericIListWrapper<T> : ICollectionWrapperIndex
    {
        IList<T> BaseCollection;

        public Type ElementType => typeof(T);
        public void AddItem(object item) => BaseCollection.Add((T)item);
        public object GetAtIndex(int index) => BaseCollection[index];
        public void SetCollection(object collection) => BaseCollection = (IList<T>)collection;
    }

    public class GenericICollectionWrapper<T> : ICollectionWrapperEnumerator
    {
        ICollection<T> BaseCollection;

        public Type ElementType => typeof(T);
        public void AddItem(object item) => BaseCollection.Add((T)item);
        public IEnumerator GetEnumerator() => BaseCollection.GetEnumerator();
        public void SetCollection(object collection) => BaseCollection = (IList<T>)collection;
    }

    public class NonGenericIListWrapper : ICollectionWrapperIndex
    {
        IList BaseCollection;

        public Type ElementType => typeof(object);

        public void AddItem(object item) => BaseCollection.Add(item);

        public bool UseIndex => true;
        public object GetAtIndex(int index) => BaseCollection[index];
        public IEnumerator GetEnumerator() => throw new Exception("ABSAVE: An attempt was made to get an enumerator on an IList collection wrapper.");
    }
}
