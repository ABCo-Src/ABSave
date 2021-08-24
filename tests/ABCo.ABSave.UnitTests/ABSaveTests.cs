using ABCo.ABSave.Configuration;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ABCo.ABSave.UnitTests
{
    [TestClass]
    public class ABSaveTests : TestBase
    {
        [TestMethod]
        public void ByteArray_Header_NoVersioning()
        {
            var map = ABSaveMap.Get<string>(ABSaveSettings.ForSize);

            byte[] arr = ABSaveConvert.Serialize("A", map);
            CollectionAssert.AreEqual(new byte[] { 0x41, 65 }, arr);
            Assert.AreEqual("A", ABSaveConvert.Deserialize<string>(arr, map));
        }

        [TestMethod]
        public void ByteArray_Header_Versioning()
        {
            var map = ABSaveMap.Get<string>(ABSaveSettings.ForSize);

            byte[] arr = ABSaveConvert.Serialize("B", map, true);
            CollectionAssert.AreEqual(new byte[] { 0xC0, 1, 66 }, arr);
            Assert.AreEqual("B", ABSaveConvert.Deserialize<string>(arr, map));
        }

        [TestMethod]
        public void ByteArray_NoHeader_NoVersioning()
        {
            var map = ABSaveMap.Get<string>(ABSaveSettings.ForSize.Customize(s => s.SetIncludeVersioningHeader(false)));

            byte[] arr = ABSaveConvert.Serialize("A", map);
            CollectionAssert.AreEqual(new byte[] { 0x81, 65 }, arr);
            Assert.AreEqual("A", ABSaveConvert.Deserialize<string>(arr, map, false));
        }

        [TestMethod]
        public void ByteArray_NoHeader_Versioning()
        {
            var map = ABSaveMap.Get<string>(ABSaveSettings.ForSize.Customize(s => s.SetIncludeVersioningHeader(false)));

            byte[] arr = ABSaveConvert.Serialize("B", map, true);
            CollectionAssert.AreEqual(new byte[] { 0x80, 1, 66 }, arr);
            Assert.AreEqual("B", ABSaveConvert.Deserialize<string>(arr, map, true));
        }

        [TestMethod]
        public void ByteArray_NoHeader_NoSetVersioning()
        {
            var map = ABSaveMap.Get<string>(ABSaveSettings.ForSize.Customize(s => s.SetIncludeVersioningHeader(false)));
            Assert.ThrowsException<Exception>(() => ABSaveConvert.Deserialize<string>(new byte[] { 0 }, map));
        }

        [TestMethod]
        public void ByteArray_Header_SetVersioning()
        {
            var map = ABSaveMap.Get<string>(ABSaveSettings.ForSize);
            Assert.ThrowsException<Exception>(() => ABSaveConvert.Deserialize<string>(new byte[] { 0 }, map, false));
        }
    }
}
