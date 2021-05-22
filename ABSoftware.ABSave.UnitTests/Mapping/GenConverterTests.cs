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

            Generator.GetExistingOrAddNull(typeof(SimpleClass));
            Assert.IsNull(ConverterMapper.TryGenerate(typeof(SimpleClass), Generator));
        }

        [TestMethod]
        public void Converter_Exact()
        {
            Setup();

            Generator.GetExistingOrAddNull(typeof(string));
            var res = ConverterMapper.TryGenerate(typeof(string), Generator);

            Assert.IsNotNull(res);
            Assert.AreEqual(TextConverter.Instance, ((ConverterContext)res).Converter);
        }

        [TestMethod]
        public void Converter_NonExact()
        {
            Setup();

            Generator.GetExistingOrAddNull(typeof(KeyValuePair<int, int>));
            var res = ConverterMapper.TryGenerate(typeof(KeyValuePair<int, int>), Generator);
 
            Assert.IsNotNull(res);
            Assert.AreEqual(KeyValueConverter.Instance, ((ConverterContext)res).Converter);
        }
    }
}