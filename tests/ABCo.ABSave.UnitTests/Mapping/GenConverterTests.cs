using ABCo.ABSave.Converters;
using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace ABCo.ABSave.UnitTests.Mapping
{
    [TestClass]
    public class GenConverterTests : MapTestBase
    {
        [TestMethod]
        public void NoConverter()
        {
            Setup();

            Generator.GetExistingOrAddNull(typeof(UnserializableClass));
            Assert.IsNull(Generator.TryGenerateConverter(typeof(UnserializableClass)));
        }

        [TestMethod]
        public void Converter_Exact()
        {
            Setup();

            Generator.GetExistingOrAddNull(typeof(string));
            var res = Generator.TryGenerateConverter(typeof(string));

            Assert.IsNotNull(res);
            Assert.IsInstanceOfType((Converter)res, typeof(TextConverter));
        }

        [TestMethod]
        public void Converter_NonExact()
        {
            Setup();

            Generator.GetExistingOrAddNull(typeof(KeyValuePair<int, int>));
            var res = Generator.TryGenerateConverter(typeof(KeyValuePair<int, int>));

            Assert.IsNotNull(res);
            Assert.IsInstanceOfType((Converter)res, typeof(KeyValueConverter));
        }
    }
}