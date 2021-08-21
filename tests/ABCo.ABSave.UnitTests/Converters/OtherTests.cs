using ABCo.ABSave.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace ABCo.ABSave.UnitTests.Converters
{
    [TestClass]
    public class OtherTests : ConverterTestBase
    {
        [TestMethod]
        public void Guid()
        {
            Setup<Guid>(ABSaveSettings.ForSpeed);
            var guid = new Guid("01234567-89ab-0123-4567-89abcdef0123");

            DoSerialize(guid);
            AssertAndGoToStart(Concat(0, guid.ToByteArray()));

            Assert.AreEqual(guid, DoDeserialize<Guid>());
        }

        [TestMethod]
        public void DateTime()
        {
            Setup<DateTime>(ABSaveSettings.ForSpeed);
            var dateTime = new DateTime(1989, 6, 3, 7, 3, 8);

            DoSerialize(dateTime);
            AssertAndGoToStart(Concat(0, BitConverter.GetBytes(dateTime.Ticks)));

            Assert.AreEqual(dateTime, DoDeserialize<DateTime>());
        }

        [TestMethod]
        public void TimeSpan()
        {
            Setup<TimeSpan>(ABSaveSettings.ForSpeed);
            var timeSpan = new TimeSpan(19, 7, 3, 8);

            DoSerialize(timeSpan);
            AssertAndGoToStart(Concat(0, BitConverter.GetBytes(timeSpan.Ticks)));

            Assert.AreEqual(timeSpan, DoDeserialize<TimeSpan>());
        }

        [TestMethod]
        public void KeyValue()
        {
            Setup<KeyValuePair<byte, bool>>(ABSaveSettings.ForSpeed);
            var obj = new KeyValuePair<byte, bool>(234, true);

            DoSerialize(obj);
            AssertAndGoToStart(0, 0, 234, 0, 0x80);

            Assert.AreEqual(obj, DoDeserialize<KeyValuePair<byte, bool>>());
        }

        [TestMethod]
        public void Version()
        {
            Setup<Version>(ABSaveSettings.ForSpeed);

            Version[] versions = new Version[]
            {
                new Version(1258215, 567, 0, 0),
                new Version(1258215, 0, 0, 0),
                new Version(1, 1258215, 0, 0),
                new Version(1, 0, 1258215, 0),
                new Version(1, 0, 0, 1258215)
            };

            for (int i = 0; i < versions.Length; i++)
            {
                DoSerialize(versions[i]);
            }

            GoToStart();

            for (int i = 0; i < versions.Length; i++)
            {
                Assert.AreEqual(versions[i], DoDeserialize<Version>());
            }
        }
    }
}
