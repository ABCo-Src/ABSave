using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Helpers
{
    /// <summary>
    /// A list that's designed to be loaded with items once and then always have its items accessed
    /// with "ref" from that point onwards.
    /// </summary>
    public struct LoadOnceList<T> where T : struct
    {
        T[] _items;
        public int Length;

        public LoadOnceList(T[] items) => (Length, _items) = (0, items);
        public void Clear() => Length = 0;
        public T[] ReleaseBuffer()
        {
            T[] items = _items;
            _items = null;
            return items;
        }

        public ref T CreateAndGet()
        {
            if (_items.Length == Length) Array.Resize(ref _items, Length * 2);
            _items[Length] = new T();
            return ref _items[Length++];
        }

        public ref T this[int index]
        {
            get => ref _items[index];
        }
    }
}
