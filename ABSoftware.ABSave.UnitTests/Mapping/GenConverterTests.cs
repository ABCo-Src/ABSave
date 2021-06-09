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
            Assert.IsNull(Generator.TryGenerateConverter(typeof(SimpleClass)));
        }

        [TestMethod]
        public void Converter_Exact()
        {
            Setup();

            Generator.GetExistingOrAddNull(typeof(string));
            var res = Generator.TryGenerateConverter(typeof(string));

            Assert.IsNotNull(res);
            Assert.AreEqual(TextConverter.Instance, ((ConverterContext)res)._converter);
        }

        [TestMethod]
        public void Converter_NonExact()
        {
            Setup();

            Generator.GetExistingOrAddNull(typeof(KeyValuePair<int, int>));
            var res = Generator.TryGenerateConverter(typeof(KeyValuePair<int, int>));
 
            Assert.IsNotNull(res);
            Assert.AreEqual(KeyValueConverter.Instance, ((ConverterContext)res)._converter);
        }
    }
}