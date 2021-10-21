using ABCo.ABSave.Configuration;
using ABCo.ABSave.Serialization.Writing;
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

            Serializer.WriteBitOn();

            if (overflow)
            {
                for (int i = 0; i < 8; i++)
                {
                    Serializer.WriteBitOn();
                }
            }

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

            Serializer.WriteBitOff();

            if (overflow)
            {
                for (int i = 0; i < 8; i++)
                {
                    Serializer.WriteBitOff();
                }
            }

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

            Serializer.WriteInteger(48, 6);
            Serializer.WriteInteger(2, 2);

            AssertAndGoToStart(194);
        }

        [TestMethod]
        public void WriteInteger_Overflow()
        {
            Initialize(ABSaveSettings.ForSpeed);

            Serializer.WriteInteger(0, 4);
            Serializer.WriteInteger(42, 6);

            AssertAndGoToStart(10, 128);
        }

        [TestMethod]
        public void FillRemainingWith()
        {
            Initialize(ABSaveSettings.ForSpeed);

            Serializer.FillRemainderOfCurrentByteWith(2);
            Serializer.Flush();
            AssertAndGoToStart(2);
        }

        [TestMethod]
        public void FreeBits()
        {
            Initialize(ABSaveSettings.ForSize);

            Serializer.WriteInteger(0, 4);
            Assert.AreEqual(Serializer.CurrentByteFreeBits, 4);
            Serializer.WriteBitOff();
            Serializer.WriteBitOn();
            Assert.AreEqual(Serializer.CurrentByteFreeBits, 2);
            Serializer.WriteInteger(42, 6);
            Assert.AreEqual(Serializer.CurrentByteFreeBits, 4);
        }
    }
}
