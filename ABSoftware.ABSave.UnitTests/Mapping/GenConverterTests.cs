using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Mapping.Generation;
using ABSoftware.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABSoftware.ABSave.UnitTests.Mapping
{
    [TestClass]
    public class GenConverterTests : MapTestBase
    {
        [TestMethod]
        public void NoConverter()
        {
            Setup();

            MapItemInfo info = Generator.CreateItem(typeof(SimpleClass), Map.GenInfo.AllTypes);
            Assert.IsFalse(ConverterMapper.TryGenerateConvert(typeof(SimpleClass), Generator, info));
        }

        [TestMethod]
        public void Converter_Exact()
        {
            Setup();

            MapItemInfo info = Generator.CreateItem(typeof(string), Map.GenInfo.AllTypes);
            Assert.IsTrue(ConverterMapper.TryGenerateConvert(typeof(string), Generator, info));

            ref MapItem item = ref Map.GetItemAt(info);
            ref ConverterMapItem converterItem = ref item.Main.Converter;

            Assert.AreEqual(TextConverter.Instance, converterItem.Converter);
            Assert.IsNotNull(converterItem.Context);
        }

        [TestMethod]
        public void Converter_NonExact()
        {
            Setup();

            MapItemInfo info = Generator.CreateItem(typeof(KeyValuePair<int, int>), Map.GenInfo.AllTypes);
            Assert.IsTrue(ConverterMapper.TryGenerateConvert(typeof(KeyValuePair<int, int>), Generator, info));

            ref MapItem item = ref Map.GetItemAt(info);
            ref ConverterMapItem converterItem = ref item.Main.Converter;

            Assert.AreEqual(KeyValueConverter.Instance, converterItem.Converter);
            Assert.IsNotNull(converterItem.Context);
        }
    }
}
