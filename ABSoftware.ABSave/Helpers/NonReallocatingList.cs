using ABSoftware.ABSave.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace ABSoftware.ABSave.Helpers
{
    /// <summary>
    /// A list that's able to grow with reallocations, by organising the data into individually allocated, variable-sized chunks. New chunks are added as necessary.
    /// Access speed is prioritized over 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal struct NonReallocatingList<T> where T : struct
    {
        // A single chunk can never be smaller than 16 items or bigger than 256 items.
        struct FixedCapacityList
        {
            public int Filled;
            readonly T[] _data;

            public FixedCapacityList(T[] data) => (Filled, _data) = (0, data);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref T CreateAndGet()
            {
                ref T d = ref _data[Filled];
                Interlocked.Increment(ref Filled);
                return ref d;
            }

            public int Capacity => _data.Length;
            public bool IsFilled => Filled == Capacity;
        }

        // Surprisingly, it's not all that common that a type will have more than 16 unique things inside it. 
        // And if does, it's not a big deal to do one more allocation. So this should be plenty.
        public const int BaseChunkSize = 16;

        internal int _totalCapacity; // Internal for testing
        int _capacityBeforeCurrentChunk;

        object _chunkStateChangingLock;

        volatile int _currentChunkIndex;
        FixedCapacityList _currentChunk;

        int _noOfChunks;
        T[][] _chunks;

        public void Initialize()
        {
            T[] firstChunk = new T[BaseChunkSize];

            _chunks = new T[][] { firstChunk };
            _noOfChunks = 1;
            _chunkStateChangingLock = new object();
            _currentChunk = new FixedCapacityList(firstChunk);
            _totalCapacity = BaseChunkSize;
        }

        public ref T GetItemRef(NonReallocatingListPos info)
        {
            return ref _chunks[info.Chunk][info.ChunkPos];
        }

        public void EnsureCapacity(int requiredSpace)
        {
        Retry:
            int requiredIndex = _capacityBeforeCurrentChunk + _currentChunk.Filled + requiredSpace;
            int currentTotalCapacity = _totalCapacity;

            if (requiredIndex > currentTotalCapacity)
            {
                lock (_chunkStateChangingLock)
                {
                    // If something changed while we were waiting, try again.
                    if (currentTotalCapacity != _totalCapacity) goto Retry;

                    int needed = requiredIndex - _totalCapacity;
                    int toAllocate = needed + BaseChunkSize;

                    // Chunks can't be more than 256 items big. So if we do try to allocate more than that, 
                    // we need to make multiple chunks.
                    if (toAllocate > 256)
                    {
                        var quot = Math.DivRem(toAllocate, 256, out int remainder);

                        // Allocate all the 256-size chunks we need.
                        for (int i = 0; i < quot; i++) AddChunk(new T[256]);

                        // Allocate one last chunk with the left over data, if there is any.
                        if (remainder != 0) AddChunk(new T[remainder]);
                    }
                    else AddChunk(new T[toAllocate]);

                    _totalCapacity += toAllocate;
                }
            }
        }

        public ref T CreateItemAndGet(out NonReallocatingListPos pos)
        {
            GetNewPos(out int chunk, out int chunkPos);

            pos = new NonReallocatingListPos((ushort)chunk, (byte)chunkPos);
            return ref _chunks[chunk][chunkPos];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe void GetNewPos(out int chunk, out int subPos)
        {
        Retry:
            chunk = _currentChunkIndex;

            if (_currentChunk.Filled >= _chunks[chunk].Length)
            {
                lock (_chunkStateChangingLock)
                {
                    // Now that we've taken the lock, see if the situation was resolved by someone
                    // else while we were waiting. Start again if it has.
                    if (chunk != _currentChunkIndex) goto Retry;

                    // Make sure there's at least 1 free space in the next chunk.
                    EnsureCapacityHas1();

                    // Move to the next chunk.
                    chunk = ++_currentChunkIndex;
                    _capacityBeforeCurrentChunk += _currentChunk.Capacity;
                    _currentChunk = new FixedCapacityList(_chunks[_currentChunkIndex])
                    {
                        Filled = 1
                    };
                }

                subPos = 0;
            }
            else subPos = Interlocked.Increment(ref _currentChunk.Filled) - 1;
        }

        void EnsureCapacityHas1()
        {
            int totalIndex = _capacityBeforeCurrentChunk + _currentChunk.Filled;
            if (totalIndex == _totalCapacity)
            {
                // Add a new chunk
                AddChunk(new T[BaseChunkSize]);
                _totalCapacity += BaseChunkSize;
            }
        }

        void AddChunk(T[] arr)
        {
            if (_noOfChunks == _chunks.Length)
            {
                T[][] newChunks = new T[_chunks.Length * 2][];
                Array.Copy(_chunks, newChunks, _chunks.Length);
                _chunks = newChunks;
            }

            _chunks[_noOfChunks++] = arr;
        }
    }

    internal struct NonReallocatingListPos : IEquatable<NonReallocatingListPos>
    {
        public ushort Chunk;
        public byte ChunkPos;

        // A flag that can be dedicated to anything.
        public bool Flag;

        public NonReallocatingListPos(ushort chunk, byte chunkPos) => (Chunk, ChunkPos, Flag) = (chunk, chunkPos, false);
        public NonReallocatingListPos(bool flag) => (Chunk, ChunkPos, Flag) = (0, 0, flag);

        public override int GetHashCode() => (Chunk << 16) | (Flag ? 256 : 0) | ChunkPos;
        public override bool Equals(object? obj) => obj is NonReallocatingListPos pos && Equals(pos);
        public bool Equals(NonReallocatingListPos other) =>
            Chunk == other.Chunk && ChunkPos == other.ChunkPos && Flag == other.Flag;
    }
}
