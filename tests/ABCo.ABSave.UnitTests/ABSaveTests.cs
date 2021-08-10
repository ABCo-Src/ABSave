using ABCo.ABSave.Configuration;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ABCo.ABSave.UnitTests
{
    [TestClass]
    public class ABSaveTests : TestBase
    {
        [TestMethod]
        public void ByteArray_NoVersioning()
        {
            var map = ABSaveMap.Get<string>(ABSaveSettings.ForSize);

            byte[] arr = ABSaveConvert.Serialize("A", map);
            CollectionAssert.AreEqual(new byte[] { 0x61, 65 }, arr);
            Assert.AreEqual("A", ABSaveConvert.Deserialize<string>(arr, map));
        }

        [TestMethod]
        public void ByteArray_Versioning()
        {
            var map = ABSaveMap.Get<string>(ABSaveSettings.ForSize);

            byte[] arr = ABSaveConvert.Serialize("B", map, true);
            CollectionAssert.AreEqual(new byte[] { 0xE0, 1, 66 }, arr);
            Assert.AreEqual("B", ABSaveConvert.Deserialize<string>(arr, map));
        }
    }
}
