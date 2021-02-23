using ABSoftware.ABSave.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABSoftware.ABSave.UnitTests.Helpers
{
    public struct TestClass
    {
        public int A;
    }

    [TestClass]
    public class NonReallocatingListTests
    {
        NonReallocatingList<TestClass> _list;

        [TestMethod]
        public void AddAndRetrieve_InChunk()
        {
            _list = new NonReallocatingList<TestClass>();
            _list.Initialize();

            ref TestClass itm = ref _list.CreateItemAndGet(out NonReallocatingListPos pos);
            itm.A = 8;

            Assert.AreEqual(itm, _list.GetItemRef(pos));
        }

        [TestMethod]
        public void AddAndRetrieve_CrossChunk_New()
        {
            _list = new NonReallocatingList<TestClass>();
            _list.Initialize();

            // Fill all the spaces of the first chunk.
            for (int i = 0; i < NonReallocatingList<TestClass>.BaseChunkSize; i++)
                _list.CreateItemAndGet(out _);

            ref TestClass itm = ref _list.CreateItemAndGet(out NonReallocatingListPos pos);
            itm.A = 17;

            Assert.AreEqual(1, pos.Chunk);
            Assert.AreEqual(itm, _list.GetItemRef(pos));

            // Repeat again.
            for (int i = 0; i < NonReallocatingList<TestClass>.BaseChunkSize - 1; i++)
                _list.CreateItemAndGet(out _);
             
            ref TestClass againItm = ref _list.CreateItemAndGet(out NonReallocatingListPos againPos);
            itm.A = 58;

            Assert.AreEqual(2, againPos.Chunk);
            Assert.AreEqual(againItm, _list.GetItemRef(againPos));
        }

        [TestMethod]
        public void AddAndRetrieve_CrossChunk_ExistingOne()
        {
            _list = new NonReallocatingList<TestClass>();
            _list.Initialize();

            // Prepare ONE extra chunk that we will overflow into.
            // This chunk will be "4 + BaseChunkSize" big.
            _list.EnsureCapacity(20);

            // Fill all the spaces of the first chunk.
            for (int i = 0; i < NonReallocatingList<TestClass>.BaseChunkSize; i++)
                _list.CreateItemAndGet(out _);

            // Ensure the chunk is 20 items big and those items go in fine.
            for (int i = 0; i < 4 + NonReallocatingList<TestClass>.BaseChunkSize; i++)
            {
                _list.CreateItemAndGet(out NonReallocatingListPos pos);
                Assert.AreEqual(1, pos.Chunk);
                Assert.AreEqual(i, pos.ChunkPos);
            }

            // Go beyond the chunk.
            _list.CreateItemAndGet(out NonReallocatingListPos beyondPos);
            Assert.AreEqual(2, beyondPos.Chunk);
        }

        [TestMethod]
        public void AddAndRetrieve_CrossChunk_ExistingMultiple()
        {
            _list = new NonReallocatingList<TestClass>();
            _list.Initialize();

            // Prepare multiple extra chunk that we will overflow into.
            // One "255" chunk and another "194" chunk.
            _list.EnsureCapacity(450);

            // Fill all the spaces of the first chunk.
            for (int i = 0; i < NonReallocatingList<TestClass>.BaseChunkSize; i++)
                _list.CreateItemAndGet(out _);

            // Fill the 255 chunk.
            for (int i = 0; i < 256; i++)
            {
                _list.CreateItemAndGet(out NonReallocatingListPos pos);
                Assert.AreEqual(1, pos.Chunk);
                Assert.AreEqual(i, pos.ChunkPos);
            }

            // Fill the other chunk.
            for (int i = 0; i < 194; i++)
            {
                _list.CreateItemAndGet(out NonReallocatingListPos pos);
                Assert.AreEqual(2, pos.Chunk);
                Assert.AreEqual(i, pos.ChunkPos);
            }

            // Overflow into a new chunk.
            _list.CreateItemAndGet(out NonReallocatingListPos overflowPos);
            Assert.AreEqual(3, overflowPos.Chunk);
        }
    }
}
