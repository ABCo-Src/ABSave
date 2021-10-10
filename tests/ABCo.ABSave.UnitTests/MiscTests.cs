using ABCo.ABSave.Configuration;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Serialization.Converters;
using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ABCo.ABSave.UnitTests
{
    [TestClass]
    public class MiscTests : TestBase
    {
        [TestMethod]
        public void WaitUntilNotGenerating_NotGenerating()
        {
            var dummy = new DummyConverter
            {
                _isGenerating = true
            };

            bool hitEnd = false;

            Task tsk = Task.Run(async () =>
            {
                await Task.Delay(500);

                if (hitEnd) throw new Exception("Failed!");
                dummy._isGenerating = false;
            });

            ABSaveUtils.WaitUntilNotGenerating(dummy);
            hitEnd = true;
            tsk.Wait();
        }

        [TestMethod]
        public void LightConcurrentPool_Empty()
        {
            var pool = new LightConcurrentPool<string>(4);
            Assert.IsNull(pool.TryRent());
        }

        [TestMethod]
        public void LightConcurrentPool_ExistingItem()
        {
            var pool = new LightConcurrentPool<string>(4);
            pool.Release("abc");
            Assert.AreEqual("abc", pool.TryRent());
        }

        [TestMethod]
        public void LightConcurrentPool_Filled()
        {
            var pool = new LightConcurrentPool<string>(4);
            pool.Release("abc");
            pool.Release("def");
            pool.Release("ghi");
            pool.Release("jkl");
            pool.Release("mno");

            Assert.AreEqual("jkl", pool.TryRent());
            Assert.AreEqual("ghi", pool.TryRent());
            Assert.AreEqual("def", pool.TryRent());
            Assert.AreEqual("abc", pool.TryRent());
        }

        [TestMethod]
        public void SettingsBuilder_CustomValues()
        {
            var settings = ABSaveSettings.ForSpeed.Customize(b => b
                .SetUseUTF8(false)
                .SetUseLittleEndian(false)
                .SetLazyWriteCompressed(false)
                .SetIncludeVersioningHeader(true)
                .SetCompressPrimitives(true));

            Assert.IsFalse(settings.UseUTF8);
            Assert.IsFalse(settings.UseLittleEndian);
            Assert.IsFalse(settings.LazyCompressedWriting);
            Assert.IsTrue(settings.IncludeVersioningHeader);
            Assert.IsTrue(settings.CompressPrimitives);
        }
    }

    class DummyConverter : Converter
    {
        public override object Deserialize(in DeserializeInfo info)
        {
            throw new NotImplementedException();
        }

        public override void Serialize(in SerializeInfo info)
        {
            throw new NotImplementedException();
        }
    }
}
