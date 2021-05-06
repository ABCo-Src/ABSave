using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ABSoftware.ABSave.Helpers
{
    internal class LightConcurrentPool<T> where T : class
    {
        int _itemCount;
        readonly T[] _items;

        public LightConcurrentPool(int maxCapacity) => _items = new T[maxCapacity];

        public T TryRent()
        {
            lock (_items)
            {
                if (_itemCount == 0) return null;
                return _items[_itemCount--];
            }
        }

        public void Release(T item)
        {
            lock (_items)
            {
                if (_itemCount == _items.Length) return;
                _items[_itemCount++] = item;
            }
        }
    }
}