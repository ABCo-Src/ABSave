﻿using ABCo.ABSave.Converters;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Generation;
using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABCo.ABSave.UnitTests.Mapping
{
    [TestClass]
    public class GenConverterTests : MapTestBase
    {
        [TestMethod]
        public void NoConverter()
        {
            Setup();

            Generator.GetExistingOrAddNull(typeof(AllPrimitiveClass));
            Assert.IsNull(Generator.TryGenerateConverter(typeof(AllPrimitiveClass)));
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