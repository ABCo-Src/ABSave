using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABSoftware.ABSave.UnitTests.Core
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
            ResetOutput();

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
        [DataRow(false)]
        [DataRow(true)]
        public void ReadInteger(bool lazy)
        {
            Initialize(lazy ? ABSaveSettings.GetSpeedFocus(false) : ABSaveSettings.GetSizeFocus(false));

            var source = new BitSource(lazy ? 0b11000000 : 0b11000110, Deserializer);
            // Setup the next byte too
            Stream.WriteByte(lazy ? 0b01100100 : 0b01000000);
            GoToStart();

            Assert.AreEqual(12, source.ReadInteger(4));
            Assert.AreEqual(25, source.ReadInteger(6));
        }
    }
}
