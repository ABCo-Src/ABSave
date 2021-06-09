using ABCo.ABSave.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABCo.ABSave.UnitTests.Helpers
{
    [TestClass]
    public class LoadOnceListTests
    {
        [TestMethod]
        public void Add_OneChunk()
        {
            TestAdd(3);
        }

        [TestMethod]
        public void Add_TwoChunks()
        {
            TestAdd(18);
        }

        [TestMethod]
        public void Add_ThreeChunks()
        {
            TestAdd(38);
        }

        static void TestAdd(int size)
        {
            var lst = new LoadOnceList<int>();
            lst.Initialize();

            for (int i = 0; i < size; i++)
                lst.CreateAndGet() = i;

            var arr = lst.ReleaseAndGetArray();

            Assert.AreEqual(size, arr.Length);
            for (int i = 0; i < size; i++)
                if (arr[i] != i)
                    throw new Exception($"Item {i} not matching!");
        }
    }
}
