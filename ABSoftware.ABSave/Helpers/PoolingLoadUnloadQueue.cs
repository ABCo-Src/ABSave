using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Helpers
{
    ///// <summary>
    ///// A chunk-based queue that's designed for being filled with items once, and then having those items all removed again.
    ///// Each chunk is retrieved and removed using a pool, and are removed while dequeuing reads them all up.
    ///// This allows using this structure in a recursive scenario to have reduced allocations.
    ///// </summary>
    //class PoolingLoadUnloadQueue<T>
    //{
    //    // Choosing the chunk size is difficult. If we put it too low, then we'll be doing a lot of
    //    // allocations. And if we put it too high, any recursive use of this queue won't be able to use
    //    // the pooled arrays so well.
    //    public const int ChunkSize = 4;

    //    // When we're enqueuing, this is the chunk we're currently enqueuing to.
    //    // When we're dequeuing, this is the last chunk.
    //    int _lastChunkWritten;
    //    T[] _lastChunk;

    //    // When we're dequeuing, this is the chunk we're currently dequeuing.
    //    int _currentDequeueCapacity;
    //    int _currentDequeueRead;
    //    T[] _currentDequeue;

    //    Queue<T[]> _chunks = new Queue<T[]>();

    //    public PoolingLoadUnloadQueue()
    //    {
    //        _lastChunk = LightConcurrentPool<T[]>.TryRent() ?? new T[ChunkSize];
    //    }

    //    public void Enqueue(T item)
    //    {
    //        if (_lastChunkWritten == ChunkSize) EnqueueNewChunk();
    //        _lastChunk[_lastChunkWritten++] = item;
    //    }

    //    public void PrepareForDequeue()
    //    {
    //        _currentDequeueCapacity = ChunkSize;
    //    }

    //    public bool Dequeue(ref T item)
    //    {
    //        if (_currentDequeueRead == _currentDequeueCapacity) return DequeueChunk();
    //        item = ref _currentDequeue[_currentDequeueRead++];
    //        return true;
    //    }

    //    void EnqueueNewChunk()
    //    {
    //        // Save our current chunk.
    //        _chunks.Enqueue(_lastChunk);
    //        _lastChunk = LightConcurrentPool<T[]>.TryRent() ?? new T[ChunkSize];
    //        _lastChunkWritten = 0;
    //    }

    //    bool DequeueChunk()
    //    {
    //        LightConcurrentPool<T[]>.Release(_currentDequeue);

    //        if (_chunks.TryDequeue(out _currentDequeue)) return true;

    //        // Load up the last chunk, if there is one. If we've used it up, we're with everything.
    //        if (_lastChunk == null) return false;

    //        _currentDequeue = _lastChunk;
    //        _currentDequeueCapacity = _lastChunkWritten;
    //        _lastChunk = null;
    //        return true;
    //    }
    //}
}
