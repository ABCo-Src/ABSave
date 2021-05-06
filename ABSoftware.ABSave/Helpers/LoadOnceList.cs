using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ABSoftware.ABSave.Helpers
{
    /// <summary>
    /// A list that's designed to be loaded with items sequentially once and then have an array pulled out.
    /// </summary>
    public struct LoadOnceList<T> where T : struct
    {
        static readonly LightConcurrentPool<Block> BlockPool = new LightConcurrentPool<Block>(4);

        const int BLOCK_SIZE = 8;

        // While we're adding items, we're going to add them in blocks.
        Block _startBlock;
        Block _currentBlock;

        int _blockCountBeforeCurrent;
        int _currentBlockFilled;

        public void Initialize()
        {
            _startBlock = _currentBlock = BlockPool.TryRent() ?? new Block();
            _blockCountBeforeCurrent = 0;
            _currentBlockFilled = 0;
        }

        // Inline to try and reduce overhead of "new T"
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe T[] ReleaseAndGetArray()
        {
            int totalLen = (_blockCountBeforeCurrent * BLOCK_SIZE) + _currentBlockFilled;

            // If we've managed to somehow EXACTLY fill a single block, we'll just use the data from that.
            if (totalLen == BLOCK_SIZE) return _startBlock.Data;

            var res = ABSaveUtils.CreateUninitializedArray<T>(totalLen);

            // Go through every full block and copy the data out of them into our final array.
            int currentPos = 0;
            for (Block block = _startBlock; block != _currentBlock; block = block.Next)
            {
                Array.Copy(block.Data, 0, res, currentPos, block.Data.Length);

                // Release it
                Release(block);

                currentPos += BLOCK_SIZE;
            }

            // Copy the data out of the latest block.
            Array.Copy(_currentBlock.Data, 0, res, currentPos, _currentBlockFilled);

            return res;
        }

        public ref T CreateAndGet()
        {
            // Add a new block if we've reached the capacity.
            if (_currentBlockFilled == BLOCK_SIZE)
            {
                Block newBlock = BlockPool.TryRent() ?? new Block();
                _currentBlock = _currentBlock.Next = newBlock;
                _currentBlockFilled = 0;
                _blockCountBeforeCurrent++;
            }

            return ref _currentBlock.Data[_currentBlockFilled++];
        }

        static void Release(Block block)
        {
            block.Next = null;
            BlockPool.Release(block);
        }

        class Block
        {
            public T[] Data = new T[BLOCK_SIZE];
            public Block Next;
        }
    }
}
