using ABCo.ABSave.Converters;
using ABCo.ABSave.Deserialization;
using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Serialization;
using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ABCo.ABSave.UnitTests.Mapping
{
    [TestClass]
    public class MapGeneratorTests : MapTestBase
    {
        [TestMethod]
        public void Get()
        {
            Setup();

            // Generate it once.
            var pos = Generator.GetMap(typeof(AllPrimitiveClass));

            // See if it picks up on the existing item.
            Assert.AreEqual(pos.Converter, Generator.GetMap(typeof(AllPrimitiveClass)).Converter);
        }

        [TestMethod]
        public void Get_Existing()
        {
            Setup();

            // Generate it once.
            var pos = Generator.GetMap(typeof(AllPrimitiveClass));

            // See if it picks up on the existing item.
            Assert.AreEqual(pos, Generator.GetMap(typeof(AllPrimitiveClass)));
        }

        [TestMethod]
        public void GetOrAddNull_New()
        {
            Setup();
            Assert.IsNull(Generator.GetExistingOrAddNull(typeof(AllPrimitiveClass)));
            Assert.IsNull(Map._allTypes[typeof(AllPrimitiveClass)]);
        }

        [TestMethod]
        public void GetOrAddNull_Existing()
        {
            Setup();

            var pos = Generator.GetMap(typeof(AllPrimitiveClass));

            Assert.AreEqual(pos.Converter, Generator.GetExistingOrAddNull(typeof(AllPrimitiveClass)));
        }

        class EmptyConverter : Converter
        {
            public override void Serialize(in SerializeInfo info, ref BitTarget header) => throw new NotImplementedException();
            public override object Deserialize(in DeserializeInfo info) => throw new NotImplementedException();
        }

        [TestMethod]
        public async Task GetOrAddNull_WaitsOnNull()
        {
            Setup();

            // This thread will make an "Allocating" item, the "waiter" should wait for that to change.
            var secondGenerator = Map.GetGenerator();
            Converter retrieved = null;

            var waiter = new Task(() =>
            {
                retrieved = Generator.GetExistingOrAddNull(typeof(int));
            });

            // Make an "Allocating" item.
            Generator.GetExistingOrAddNull(typeof(int));

            waiter.Start();

            // Wait a second - this should be more than enough time for the waiter to be stuck in the waiting cycle.
            await Task.Delay(1000);

            // Now, we will finish the item, and see if the thread finishes accordingly.
            var newMapItem = new EmptyConverter();
            Generator.ApplyItem(newMapItem, typeof(int));

            await Task.Delay(1000);

            Assert.AreEqual(newMapItem, retrieved);
            ABSaveMap.ReleaseGenerator(secondGenerator);
        }

        [TestMethod]
        public void GetOrAddNull_TwoThreads_GenerateNew()
        {
            Setup();

            var secondGenerator = Map.GetGenerator();

            Converter first = null;
            Converter second = null;

            // Trigger both threads at exactly the same time.
            Thread tsk = new Thread(() =>
            {
                first = Generator.GetExistingOrAddNull(typeof(int));
                if (first == null)
                {
                    Generator.ApplyItem(new EmptyConverter(), typeof(int));
                }
            });

            Thread tsk2 = new Thread(() =>
            {
                second = Generator.GetExistingOrAddNull(typeof(int));
                if (second == null)
                {
                    Generator.ApplyItem(new EmptyConverter(), typeof(int));
                }
            });

            tsk.Start();
            tsk2.Start();

            tsk.Join();
            tsk2.Join();

            // Whichever raced to get the generation done doesn't matter, 
            // if one is null, the other should be not be null.
            Assert.IsNotNull(first ?? second);

            // Check that the item was created successfully.
            Assert.IsInstanceOfType(Map._allTypes[typeof(int)], typeof(EmptyConverter));
            ABSaveMap.ReleaseGenerator(secondGenerator);
        }

        [TestMethod]
        public void Generate_Nullable()
        {
            Setup();

            // Inner does not exist
            var pos = Generator.GetMap(typeof(AllPrimitiveStruct?));
            Assert.IsTrue(pos.IsNullable);

            // Inner does exist
            var pos2 = Generator.GetMap(typeof(AllPrimitiveStruct?));
            Assert.IsTrue(pos2.IsNullable);
        }
    }
}
