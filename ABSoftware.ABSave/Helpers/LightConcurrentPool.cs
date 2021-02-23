using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ABSoftware.ABSave.Helpers
{
    internal static class LightConcurrentPool<T> where T : class
    {
        static int _itemCount;
        static ConcurrentQueue<T> _items = new ConcurrentQueue<T>();

        public static T TryRent()
        {
            if (_items.TryDequeue(out T res))
            {
                Interlocked.Decrement(ref _itemCount);
                return res;
            }
            return null;
        }

        public static void Release(T item)
        {
            if (_itemCount == 4) return;

            Interlocked.Increment(ref _itemCount);
            _items.Enqueue(item);
        }
    }
}