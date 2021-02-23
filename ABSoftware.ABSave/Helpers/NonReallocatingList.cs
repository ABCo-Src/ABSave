using ABSoftware.ABSave.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace ABSoftware.ABSave.Helpers
{
    /// <summary>
    /// A list that's able to grow with reallocations, by organising the data into individually allocated, variable-sized chunks. New chunks are added necessary.
    /// Access speed is prioritized over 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal struct NonReallocatingList<T> where T : struct
    {
        struct FixedCapacityList
        {
            public int Filled;
            readonly T[] _data;

            public FixedCapacityList(T[] data) => (Filled, _data) = (0, data);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref T CreateAndGet() => ref _data[Filled++];

            public int Capacity => _data.Length;
            public bool IsFilled => Filled == Capacity;
        }

        // Surprisingly, it's not all that common that a type will have more than 16 unique things inside it. 
        // And if does, it's not a big deal to do one more allocation. So this should be plenty.
        public const int BaseChunkSize = 16;

        int _totalCapacity;
        int _capacityBeforeCurrentChunk;

        int _currentChunkIndex;
        FixedCapacityList _currentChunk;

        List<T[]> _chunks; // A single chunk can never be smaller than 16 items or bigger than 256 items.

        public void Initialize()
        {
            T[] firstChunk = new T[BaseChunkSize];

            _chunks = new List<T[]>() { firstChunk };
            _currentChunk = new FixedCapacityList(firstChunk);
            _totalCapacity = BaseChunkSize;
        }

        public ref T GetItemRef(NonReallocatingListPos info)
        {
            return ref _chunks[info.Chunk][info.ChunkPos];
        }

        // Make sure we're actually inside a chunk we can put items into.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void EnsureInChunk()
        {
            if (_currentChunk.IsFilled)
            {
                // Make sure there's at least 1 free byte in the next chunk.
                EnsureCapacityHas1();

                // Move to the next chunk.
                _currentChunkIndex++;
                _capacityBeforeCurrentChunk += _currentChunk.Capacity;
                _currentChunk = new FixedCapacityList(_chunks[_currentChunkIndex]);
                
            }
        }

        void EnsureCapacityHas1()
        {
            int totalIndex = _capacityBeforeCurrentChunk + _currentChunk.Filled;
            if (totalIndex == _totalCapacity)
            {
                _chunks.Add(new T[BaseChunkSize]);
                _totalCapacity += BaseChunkSize;
            }
        }

        public void EnsureCapacity(int requiredSpace)
        {
            int requiredIndex = _capacityBeforeCurrentChunk + _currentChunk.Filled + requiredSpace;
            if (requiredIndex > _totalCapacity)
            {
                int needed = requiredIndex - _totalCapacity;
                int toAllocate = needed + BaseChunkSize;

                // Chunks can't be more than 256 items big. So if we do try to allocate more than that, we need to make multiple chunks.
                if (toAllocate > 256)
                {
                    var quot = Math.DivRem(toAllocate, 256, out int remainder);

                    // Allocate all the 256-size chunks we need.
                    for (int i = 0; i < quot; i++) _chunks.Add(new T[256]);

                    // Allocate one last chunk with the left over data, if there is any.
                    if (remainder != 0) _chunks.Add(new T[remainder]);
                }
                else _chunks.Add(new T[toAllocate]);

                _totalCapacity += toAllocate;
            }
        }

        public NonReallocatingListPos CreateItem()
        {
            EnsureInChunk();            
            return new NonReallocatingListPos((ushort)_currentChunkIndex, (byte)_currentChunk.Filled++);
        }

        public ref T CreateItemAndGet(out NonReallocatingListPos pos)
        {
            EnsureInChunk();

            pos = new NonReallocatingListPos((ushort)_currentChunkIndex, (byte)_currentChunk.Filled);
            return ref _currentChunk.CreateAndGet();
        }
    }

    internal struct NonReallocatingListPos
    {
        // 65535, 255 => None
        public ushort Chunk;
        public byte ChunkPos;

        // A flag that can be dedicated to anything.
        public bool Flag;

        public NonReallocatingListPos(ushort chunk, byte chunkPos) => (Chunk, ChunkPos, Flag) = (chunk, chunkPos, false);
        public NonReallocatingListPos(bool flag) => (Chunk, ChunkPos, Flag) = (0, 0, flag);
    }
}
