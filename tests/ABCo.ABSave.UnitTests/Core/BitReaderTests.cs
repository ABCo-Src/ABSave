using ABCo.ABSave.Configuration;
using ABCo.ABSave.Serialization.Reading;
using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ABCo.ABSave.UnitTests.Core
{
    [TestClass]
    public class BitReaderTests : TestBase
    {
        [TestMethod]
        public void ReadBit()
        {
            Initialize();

            Stream.WriteByte(0b11000100);
            Stream.WriteByte(0b10000000);
            Stream.Position = 0;

            Assert.IsTrue(Deserializer.ReadBit());
            Assert.IsTrue(Deserializer.ReadBit());
            Assert.IsFalse(Deserializer.ReadBit());
            Assert.IsFalse(Deserializer.ReadBit());
            Assert.IsFalse(Deserializer.ReadBit());
            Assert.IsTrue(Deserializer.ReadBit());
            Assert.IsFalse(Deserializer.ReadBit());
            Assert.IsFalse(Deserializer.ReadBit());

            // Overflow
            Assert.IsTrue(Deserializer.ReadBit());
        }

        [TestMethod]
        public void ReadInteger()
        {
            Initialize(ABSaveSettings.ForSpeed);

            Stream.WriteByte(0b11000110);
            Stream.WriteByte(0b01000000);
            Stream.Position = 0;

            Assert.AreEqual(12, Deserializer.ReadInteger(4));
            Assert.AreEqual(25, Deserializer.ReadInteger(6));
        }

        [TestMethod]
        public void ReadInteger_OnEdge()
        {
            Initialize(ABSaveSettings.ForSpeed);

            Stream.WriteByte(0b10000110);
            Stream.Position = 0;

            Deserializer.ReadBit();
            Assert.AreEqual(6, Deserializer.ReadInteger(7));
        }

        [TestMethod]
        public void FillRemainingWith()
        {
            Initialize(ABSaveSettings.ForSpeed);

            Stream.WriteByte(2);
            Stream.Position = 0;

            Assert.AreEqual(2, Deserializer.ReadRestOfCurrentByte());
        }
    }
}
