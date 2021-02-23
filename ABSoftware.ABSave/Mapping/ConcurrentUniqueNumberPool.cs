using ABSoftware.ABSave.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ABSoftware.ABSave.Mapping
{
    internal class ConcurrentUniqueNumberPool
    {
        int _maxNumber;
        Stack<int> _savedNumbers = new Stack<int>();

        public int RentNumber()
        {
            lock (_savedNumbers)
            {
                if (_savedNumbers.Count > 0) return _savedNumbers.Pop();
            }

            return Interlocked.Increment(ref _maxNumber);
        }

        public void ReleaseNumber(int id)
        {
            lock (_savedNumbers)
            {
                _savedNumbers.Push(id);
            }
        }
    }

    ///// <summary>
    ///// Similar to a list, but with pooling functionality for a <see cref="MapGenerator"/>. Items are never
    ///// removed, and are instead simply marked as "ready to use" for future times we want to get a <see cref="MapGenerator"/>.
    ///// 
    ///// <para>NOTE: All operations are accessible concurrently, and locking on this instance will prevent items
    ///// from being removed, allowing map generators to be held in place for temporary amount of time.</para>
    ///// </summary>
    //internal class ConcurrentFixedPosPool<T>
    //{
    //    public ABSaveMap Map;

    //    struct DataItem
    //    {
    //        public T Item;
    //        public int RefCount;
    //    }

    //    volatile bool _releasingPaused;
    //    NonReallocatingList<DataItem> _data;
    //    Stack<NonReallocatingListPos> _freePositions = new Stack<NonReallocatingListPos>(24);

    //    public ConcurrentFixedPosPool()
    //    {
    //        _data.Initialize();
    //    }

    //    public ref T RentNew(out NonReallocatingListPos res)
    //    {
    //        NonReallocatingListPos? pos = null;

    //        lock (_freePositions)
    //        {
    //            // Get the next applicable item
    //            if (_freePositions.Count == 0)
    //                pos = _freePositions.Pop();
    //        }

    //        // Create a new item if we couldn't find anything.
    //        if (pos == null) pos = _data.CreateItem();

    //        // Get the item
    //        ref DataItem newItem = ref _data.GetItemRef(pos.Value);
    //        newItem.RefCount = 1;

    //        res = pos.Value;
    //        return ref newItem.Item;
    //    }

    //    internal void Release(NonReallocatingListPos pos)
    //    {
    //        ref DataItem item = ref _data.GetItemRef(pos);

    //        Interlocked.Decrement(ref item.RefCount);

    //        // Wait until releasing isn't paused
    //        var waiter = new SpinWait();
    //        while (_releasingPaused) waiter.SpinOnce();

    //        if (item.RefCount == 0)
    //            lock (_freePositions) _freePositions.Push(pos);
    //    }

    //    public ref T RentRefTo(NonReallocatingListPos pos)
    //    {
    //        ref DataItem item = ref _data.GetItemRef(pos);

    //        Interlocked.Increment(ref item.RefCount);
    //        return ref item.Item;
    //    }

    //    public void PauseReleasing()
    //    {
    //        _releasingPaused = true;
    //    }

    //    public void ContinueReleasing()
    //    {
    //        _releasingPaused = false;
    //    }

    //    //int ExpandCapacity(int to)
    //    //{
    //    //    int firstItem = _data.Length;

    //    //    // Allocate and copy a new aray
    //    //    DataItem[] newData = new DataItem[to];
    //    //    Array.Copy(_data, newData, newData.Length);
    //    //    _data = newData;

    //    //    // Initialize the new items' generators.
    //    //    for (int i = firstItem; i < _data.Length; i++)
    //    //        _data[i].Item = InitializeNewMapGen(i);

    //    //    return firstItem;
    //    //}
    //}
}
