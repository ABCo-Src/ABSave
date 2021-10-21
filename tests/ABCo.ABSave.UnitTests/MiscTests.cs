using ABCo.ABSave.Configuration;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description.Attributes;
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
        public void MapItemInfo_GetItemType_NonNullable()
        {
            var mii = new MapItemInfo(new DummyConverter(typeof(string)), false);
            Assert.AreEqual(typeof(string), mii.GetItemType());
        }

        [TestMethod]
        public void MapItemInfo_GetItemType_Nullable()
        {
            var mii = new MapItemInfo(new DummyConverter(typeof(int)), true);
            Assert.AreEqual(typeof(int?), mii.GetItemType());
        }

        [TestMethod]
        public void MapItemInfo_GetItemType_IsEqual()
        {
            var dummy = new DummyConverter(typeof(int));
            var mii = new MapItemInfo(dummy, true);
            var mii2 = new MapItemInfo(dummy, true);

            Assert.IsTrue(mii == mii2);
            Assert.IsFalse(mii != mii2);
            Assert.IsTrue(mii.Equals(mii2));
        }

        [TestMethod]
        public void MapItemInfo_GetItemType_IsNotEqual_BecauseNullable()
        {
            var dummy = new DummyConverter(typeof(int));
            var mii = new MapItemInfo(dummy, true);
            var mii2 = new MapItemInfo(dummy, false);

            Assert.IsFalse(mii == mii2);
            Assert.IsTrue(mii != mii2);
            Assert.IsFalse(mii.Equals(mii2));
        }

        [TestMethod]
        public void MapItemInfo_GetItemType_IsNotEqual_BecauseConverterItemType()
        {
            var mii = new MapItemInfo(new DummyConverter(typeof(int)), true);
            var mii2 = new MapItemInfo(new DummyConverter(typeof(string)), true);

            Assert.IsFalse(mii == mii2);
            Assert.IsTrue(mii != mii2);
            Assert.IsFalse(mii.Equals(mii2));
        }

        [TestMethod]
        public void MapItemInfo_GetItemType_IsNotEqual_BecauseBadComparison()
        {
            var mii = new MapItemInfo(new DummyConverter(typeof(int)), true);
            Assert.IsFalse(mii.Equals("abc"));
        }

        [TestMethod]
        public void AttributeWithVersion_CompareTo_LessThan()
        {
            var attr = new SaveAttribute
            {
                FromVer = 3
            };

            var attr2 = new SaveAttribute
            {
                FromVer = 2
            };

            Assert.AreEqual(-1, attr.CompareTo(attr2));
        }

        [TestMethod]
        public void AttributeWithVersion_CompareTo_MoreThan()
        {
            var attr = new SaveAttribute
            {
                FromVer = 3
            };

            var attr2 = new SaveAttribute
            {
                FromVer = 4
            };

            Assert.AreEqual(1, attr.CompareTo(attr2));
        }

        [TestMethod]
        public void AttributeWithVersion_CompareTo_Equal()
        {
            var attr = new SaveAttribute
            {
                FromVer = 3
            };

            var attr2 = new SaveAttribute
            {
                FromVer = 3
            };

            Assert.AreEqual(0, attr.CompareTo(attr2));
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
        public DummyConverter() { }
        public DummyConverter(Type itemType) => ItemType = itemType;

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
