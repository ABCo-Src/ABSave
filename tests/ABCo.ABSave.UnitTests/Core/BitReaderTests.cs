using ABCo.ABSave.Configuration;
using ABCo.ABSave.Serialization.Writing.Reading;
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

            var source = Deserializer.GetHeader();
            Stream.WriteByte(0b11000100);
            Stream.WriteByte(0b10000000);
            Stream.Position = 0;

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

            var source = Deserializer.GetHeader();
            Stream.WriteByte(0b11000110);
            Stream.WriteByte(0b01000000);
            Stream.Position = 0;

            Assert.AreEqual(12, source.ReadInteger(4));
            Assert.AreEqual(25, source.ReadInteger(6));
        }

        [TestMethod]
        public void ReadInteger_OnEdge()
        {
            Initialize(ABSaveSettings.ForSpeed);

            var source = Deserializer.GetHeader();
            Stream.WriteByte(0b10000110);
            Stream.Position = 0;

            source.ReadBit();
            Assert.AreEqual(6, source.ReadInteger(7));
        }
    }
}
