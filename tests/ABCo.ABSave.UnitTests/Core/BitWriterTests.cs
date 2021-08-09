using ABCo.ABSave.Configuration;
using ABCo.ABSave.Serialization;
using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ABCo.ABSave.UnitTests.Core
{
    [TestClass]
    public class BitWriterTests : TestBase
    {
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void WriteBitOn(bool overflow)
        {
            Initialize();

            var target = Serializer.GetHeader();

            target.WriteBitOn();

            if (overflow)
            {
                for (int i = 0; i < 8; i++)
                {
                    target.WriteBitOn();
                }
            }

            target.Finish();

            if (overflow)
            {
                AssertAndGoToStart(255, 128);
            }
            else
            {
                AssertAndGoToStart(128);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void WriteBitOff(bool overflow)
        {
            Initialize();

            var target = Serializer.GetHeader();

            target.WriteBitOff();

            if (overflow)
            {
                for (int i = 0; i < 8; i++)
                {
                    target.WriteBitOff();
                }
            }

            target.Finish();

            if (overflow)
            {
                AssertAndGoToStart(0, 0);
            }
            else
            {
                AssertAndGoToStart(0);
            }
        }

        [TestMethod]
        public void WriteInteger_NoOverflow()
        {
            Initialize();

            var target = Serializer.GetHeader();
            target.WriteInteger(48, 6);
            target.WriteInteger(2, 2);
            target.Finish();

            AssertAndGoToStart(194);
        }

        [TestMethod]
        public void WriteInteger_Overflow()
        {
            Initialize(ABSaveSettings.ForSpeed);

            var target = Serializer.GetHeader();
            target.WriteInteger(0, 4);
            target.WriteInteger(42, 6);
            target.Finish();

            AssertAndGoToStart(10, 128);
        }

        [TestMethod]
        public void FreeBits()
        {
            Initialize(ABSaveSettings.ForSize);
            var target = Serializer.GetHeader();

            target.WriteInteger(0, 4);
            Assert.AreEqual(target.FreeBits, 4);
            target.WriteBitOff();
            target.WriteBitOn();
            Assert.AreEqual(target.FreeBits, 2);
            target.WriteInteger(42, 6);
            Assert.AreEqual(target.FreeBits, 4);
            target.Finish();
        }
    }
}
