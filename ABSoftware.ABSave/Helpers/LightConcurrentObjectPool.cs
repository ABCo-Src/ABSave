using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ABSoftware.ABSave.Helpers
{
    public abstract class ABSavePoolable<T> : IDisposable where T : ABSavePoolable<T>, new()
    {
        internal int Index;

        public static T Create() => LightConcurrentObjectPool<T>.Rent();
        public void Dispose() => LightConcurrentObjectPool<T>.Release(Index);
    }

    internal static class LightConcurrentObjectPool<T> where T : ABSavePoolable<T>, new()
    {
        readonly static object _accessLock = new object();
        static PoolData[] _poolData = new PoolData[4];

        public static T Rent()
        {
            // TODO: Improve locking mechanism with spinlock.
            lock (_accessLock)
            {
                for (int i = 0; i < _poolData.Length; i++)
                {
                    if (_poolData[i].Available)
                    {
                        _poolData[i].Available = false;
                        return _poolData[i].Item;
                    }
                }

                int oldCapacity = _poolData.Length;
                Array.Resize(ref _poolData, _poolData.Length * 2);

                // Properly initialize the items after.
                for (int i = oldCapacity; i < _poolData.Length; i++)
                {
                    _poolData[i].Item = new T
                    {
                        Index = i
                    };
                    _poolData[i].Available = true;
                }

                _poolData[oldCapacity].Available = false;
                return _poolData[oldCapacity].Item;
            }
        }

        public static void Release(int index)
        {
            lock (_accessLock)
            {
                _poolData[index].Available = true;
            }   
        }

        struct PoolData
        {
            public T Item;
            public volatile bool Available;
        }
    }
}
