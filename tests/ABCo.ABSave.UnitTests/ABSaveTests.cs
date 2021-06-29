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
        public void Serialize_ByteArray()
        {
            var map = ABSaveMap.Get<string>(ABSaveSettings.ForSize);

            byte[] arr = ABSaveConvert.Serialize("A", map);
            Assert.AreNotEqual(0, arr.Length);
        }

        [TestMethod]
        public void Deserialize_ByteArray()
        {
            var map = ABSaveMap.Get<string>(ABSaveSettings.ForSize);
            byte[] arr = ABSaveConvert.Serialize("A", map);

            Assert.AreEqual("A", ABSaveConvert.Deserialize<string>(arr, map));
        }
    }
}
