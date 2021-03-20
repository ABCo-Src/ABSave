using ABSoftware.ABSave.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        // THREAD TESTS:
        class ThreadTestingInfo
        {
            public NonReallocatingList<TestClass> List = new NonReallocatingList<TestClass>();
            public HashSet<NonReallocatingListPos> DuplicateChecker = new HashSet<NonReallocatingListPos>();

            public ThreadTestingInfo()
            {
                List.Initialize();
            }

            public void AddThreads()
            {
                for (int i = 0; i < 100; i++)
                {
                    List.CreateItemAndGet(out NonReallocatingListPos currentPos);

                    lock (DuplicateChecker)
                    {
                        if (!DuplicateChecker.Add(currentPos)) throw new Exception("Two threads retrieved the same item!");
                    }
                }
            }

            public void EnsureCapacity()
            {
                // Yield 5 times before we finally try to ensure the capacity.
                for (int i = 0; i < 5; i++)
                    Thread.Yield();

                List.EnsureCapacity(127);
            }
        }

        [TestMethod]
        public async Task Add_MultipleThreads()
        {
            ThreadTestingInfo test = new ThreadTestingInfo();

            Task adder1 = new Task(test.AddThreads);
            Task adder2 = new Task(test.AddThreads);
            Task adder3 = new Task(test.AddThreads);

            adder1.Start();
            adder2.Start();
            adder3.Start();

            await adder1;
            await adder2;
            await adder3;
        }

        [TestMethod]
        public async Task EnsureCapacity_MultipleThreads()
        {
            ThreadTestingInfo test = new ThreadTestingInfo();

            Task task1 = new Task(test.EnsureCapacity);
            Task task2 = new Task(test.EnsureCapacity);
            Task task3 = new Task(test.EnsureCapacity);

            task1.Start();
            task2.Start();
            task3.Start();

            await task1;
            await task2;
            await task3;

            Assert.AreEqual(143, test.List._totalCapacity);
        }

        [TestMethod]
        public async Task AllOperations_MultipleThreads()
        {
            ThreadTestingInfo test = new ThreadTestingInfo();

            Task task1 = new Task(test.AddThreads);
            Task task2 = new Task(test.EnsureCapacity);
            Task task3 = new Task(test.AddThreads);

            task1.Start();
            task2.Start();
            task3.Start();

            await task1;
            await task2;
            await task3;
        }
    }
}
