using ABCo.ABSave.Configuration;
using ABCo.ABSave.Deserialization;
using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ABCo.ABSave.UnitTests.Core
{
    [TestClass]
    public class BitSourceTests : TestBase
    {
        [TestMethod]
        public void ReadBit()
        {
            Initialize();

            var source = new BitSource(0b11000100, Deserializer);
            // Setup the next byte too
            Stream.WriteByte(0b10000000);
            ResetState();

            Assert.IsTrue(source.ReadBit());
            Assert.IsTrue(source.ReadBit());
            Assert.IsFalse(source.ReadBit());
            Assert.IsFalse(source.ReadBit());
            Assert.IsFalse(source.ReadBit());
            Assert.IsTrue(source.ReadBit());
            Assert.IsFalse(source.ReadBit());
            Assert.IsFalse(source.ReadBit());

            // Overflow
            Assert.IsTrue(source.ReadBit());
        }

        [TestMethod]
        public void ReadInteger()
        {
            Initialize(ABSaveSettings.ForSpeed);

            var source = new BitSource((byte)0b11000110, Deserializer);
            // Setup the next byte too
            Stream.WriteByte((byte)0b01000000);
            GoToStart();

            Assert.AreEqual(12, source.ReadInteger(4));
            Assert.AreEqual(25, source.ReadInteger(6));
        }
    }
}
