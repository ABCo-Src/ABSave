using ABCo.ABSave.Configuration;
using ABCo.ABSave.Serialization;
using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ABCo.ABSave.UnitTests.Core
{
    [TestClass]
    public class BitTargetTests : TestBase
    {
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void WriteBitOn(bool overflow)
        {
            Initialize();

            var target = new BitTarget(Serializer);

            target.WriteBitOn();

            if (overflow)
            {
                for (int i = 0; i < 8; i++)
                {
                    target.WriteBitOn();
                }
            }

            target.Apply();

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

            var target = new BitTarget(Serializer);

            target.WriteBitOff();

            if (overflow)
            {
                for (int i = 0; i < 8; i++)
                {
                    target.WriteBitOff();
                }
            }

            target.Apply();

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
            var target = new BitTarget(Serializer);

            target.WriteInteger(48, 6);
            target.WriteInteger(2, 2);
            target.Apply();

            AssertAndGoToStart(194);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void WriteInteger_Overflow(bool lazy)
        {
            Initialize(ABSaveSettings.ForSpeed);
            var target = new BitTarget(Serializer);

            target.WriteInteger(0, 4);
            target.WriteInteger(42, 6);
            target.Apply();

            AssertAndGoToStart(10, 128);
        }

        [TestMethod]
        public void FreeBits()
        {
            Initialize(ABSaveSettings.ForSize);
            var target = new BitTarget(Serializer);

            target.WriteInteger(0, 4);
            Assert.AreEqual(target.FreeBits, 4);
            target.WriteBitOff();
            target.WriteBitOn();
            Assert.AreEqual(target.FreeBits, 2);
            target.WriteInteger(42, 6);
            Assert.AreEqual(target.FreeBits, 4);
            target.Apply();
        }
    }
}
