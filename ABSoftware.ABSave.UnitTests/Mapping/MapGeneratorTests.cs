using ABSoftware.ABSave.Exceptions;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ABSoftware.ABSave.UnitTests.Mapping
{
    [TestClass]
    public class MapGeneratorTests : MapTestBase
    {
        [TestMethod]
        public void Get()
        {
            Setup();

            // Generate it once.
            var pos = Generator.GetMap(typeof(SimpleClass));

            ref MapItem item = ref Map.GetItemAt(pos);

            // See if it picks up on the existing item.
            Assert.AreEqual(MapItemType.Object, item.MapType);
        }

        [TestMethod]
        public void Get_Existing()
        {
            Setup();

            // Generate it once.
            var pos = Generator.GetMap(typeof(SimpleClass));

            // See if it picks up on the existing item.
            Assert.AreEqual(pos, Generator.GetMap(typeof(SimpleClass)));
        }

        [TestMethod]
        public void GetOrStartGenerating_New()
        {
            Setup();
            Assert.IsFalse(Generator.GetOrStartGenerating(typeof(SimpleClass), out _, Map.GenInfo.AllTypes));
        }

        [TestMethod]
        public void GetOrStartGenerating_Existing()
        {
            Setup();

            var pos = Generator.GetMap(typeof(SimpleClass));

            Assert.IsTrue(Generator.GetOrStartGenerating(typeof(SimpleClass), out MapItemInfo newPos, Map.GenInfo.AllTypes));
            Assert.AreEqual(pos, newPos);
        }

        [TestMethod]
        public async Task GetOrStartGenerating_WaitsOnAllocating()
        {
            Setup();

            // This thread will make an "Allocating" item, the "waiter" should wait for that to change.
            var secondGenerator = Map.RentGenerator();
            bool stopped = false;

            var waiter = new Task(() =>
            {
                Generator.GetOrStartGenerating(typeof(int), out MapItemInfo pos, Map.GenInfo.AllTypes);
                stopped = true;
            });

            // Make an "Allocating" item.
            MapGenerator.TryGetItemFromDict(Map.GenInfo.AllTypes, typeof(int), MapItemState.Allocating, out MapItemInfo info);

            waiter.Start();

            // Wait a second - this should be more than enough time for the waiter to be stuck in the waiting cycle.
            await Task.Delay(1000);

            Assert.IsFalse(stopped);

            // Now, we will finish the item, and see if the thread finishes accordingly.
            Generator.CreateItem(typeof(int), Map.GenInfo.AllTypes);

            await Task.Delay(1000);

            Assert.IsTrue(stopped);
        }

        [TestMethod]
        public async Task GetOrStartGenerating_TwoThreads_SameType()
        {
            Setup();

            var secondGenerator = Map.RentGenerator();

            // Trigger both threads at exactly the same time.
            Task<bool> tsk = Task.Run(() => Generator.GetOrStartGenerating(typeof(int), out _, Map.GenInfo.AllTypes));
            Task<bool> tsk2 = Task.Run(() => Generator.GetOrStartGenerating(typeof(int), out _, Map.GenInfo.AllTypes));

            bool tskRes = await tsk;
            bool tsk2Res = await tsk2;

            // Whichever raced to get the generation done doesn't matter, 
            // if one is true, the other should be false.
            if (tskRes)
                Assert.IsFalse(tskRes ? tsk2Res : tskRes);

            // Check that the item was created successfully.
            Assert.AreEqual(MapItemState.Ready, Map.GenInfo.AllTypes[typeof(int)].State);
        }

        [TestMethod]
        public void GetOrStartGenerating_Prepared()
        {
            Setup();

            MapGenerator.TryGetItemFromDict(Map.GenInfo.AllTypes, typeof(int), MapItemState.Planned, out _);

            // See if we take up the generation of this item:
            Assert.IsFalse(Generator.GetOrStartGenerating(typeof(int), out _, Map.GenInfo.AllTypes));
        }

        [TestMethod]
        public void Generate_Nullable()
        {
            Setup(); 

            // Inner does not exist
            var pos = Generator.GetMap(typeof(SimpleStruct?));
            Assert.IsTrue(pos.Pos.Flag);

            // Inner does exist
            var pos2 = Generator.GetMap(typeof(SimpleStruct?));
            Assert.IsTrue(pos2.Pos.Flag);
        }

        [TestMethod]
        public void Generate_Runtime()
        {
            Setup();

            // Nothing already exists
            var pos = Generator.GetRuntimeMap(typeof(SimpleClass));
            ref var itm = ref Map.GetItemAt(pos);

            Assert.AreEqual(MapItemType.Runtime, itm.MapType);
            Assert.AreEqual(MapItemType.Object, Map.GetItemAt(itm.Extra.RuntimeInnerItem).MapType);

            // Runtime item already exists
            var pos2 = Generator.GetRuntimeMap(typeof(SimpleClass));

            Assert.AreEqual(pos, pos2);

            // Inner already exists
            var pos3Expected = Generator.GetMap(typeof(SimpleStruct));
            var pos3Actual = Generator.GetRuntimeMap(typeof(SimpleStruct));

            ref MapItem pos3Itm = ref Map.GetItemAt(pos3Actual);

            Assert.AreEqual(pos3Expected, pos3Itm.Extra.RuntimeInnerItem);
        }

        [TestMethod]
        public void Generate_SafetyChecks()
        {
            Setup(); 

            Assert.ThrowsException<DangerousTypeException>(() => Generator.GetMap(typeof(object)));
            Assert.ThrowsException<DangerousTypeException>(() => Generator.GetMap(typeof(ValueType)));
        }
    }
}
