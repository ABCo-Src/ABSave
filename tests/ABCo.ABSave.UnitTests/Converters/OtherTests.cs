using ABCo.ABSave.Configuration;
using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

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
        [DataRow(false)]
        [DataRow(true)]
        public void DateTime(bool compressed)
        {
            Setup<DateTime>(compressed ? ABSaveSettings.ForSize : ABSaveSettings.ForSpeed);
            var dateTime = new DateTime(1989, 6, 3, 7, 3, 8);

            DoSerialize(dateTime);
            GoToStart();
            Assert.AreEqual(dateTime, DoDeserialize<DateTime>());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void TimeSpan(bool compressed)
        {
            Setup<TimeSpan>(compressed ? ABSaveSettings.ForSize : ABSaveSettings.ForSpeed);
            var timeSpan = new TimeSpan(19, 7, 3, 8);

            DoSerialize(timeSpan);
            GoToStart();
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
        public void String()
        {
            Setup<string>(ABSaveSettings.ForSize);
            var obj = "ABC";

            DoSerialize(obj);
            AssertAndGoToStart(0, 3, 65, 66, 67);

            Assert.AreEqual(obj, DoDeserialize<string>());
        }

        [TestMethod]
        public void StringBuilder()
        {
            Setup<StringBuilder>(ABSaveSettings.ForSize);
            var obj = new StringBuilder("ABC");

            DoSerialize(obj);
            AssertAndGoToStart(0, 3, 65, 66, 67);

            Assert.AreEqual(obj.ToString(), DoDeserialize<StringBuilder>().ToString());
        }

        [TestMethod]
        public void StringBuilder_Large()
        {
            Setup<StringBuilder>(ABSaveSettings.ForSize);
            var obj = new StringBuilder(new string('a', 1024));

            DoSerialize(obj);
            GoToStart();
            Assert.AreEqual(obj.ToString(), DoDeserialize<StringBuilder>().ToString());
        }

        [TestMethod]
        public void CharArray()
        {
            Setup<char[]>(ABSaveSettings.ForSize);
            var obj = new char[3] { 'A', 'B', 'C' };

            DoSerialize(obj);
            AssertAndGoToStart(0, 3, 65, 66, 67);

            Assert.AreEqual(new string(obj), new string(DoDeserialize<char[]>()));
        }

        [TestMethod]
        public void CharArray_UTF16()
        {
            Setup<char[]>(ABSaveSettings.ForSize.Customize(b => b.SetUseUTF8(false)));
            var obj = new char[3] { 'A', 'B', 'C' };

            DoSerialize(obj);
            AssertAndGoToStart(GetByteArr(new object[] { 'A', 'B', 'C' }, 0, 3, (short)GenType.Numerical, (short)GenType.Numerical, (short)GenType.Numerical));
            Assert.AreEqual(new string(obj), new string(DoDeserialize<char[]>()));
        }

        [TestMethod]
        public void Boolean()
        {
            Setup<bool>(ABSaveSettings.ForSpeed);

            Serializer.WriteItem(true, CurrentMapItem);
            Serializer.WriteItem(true, CurrentMapItem);
            Serializer.WriteItem(false, CurrentMapItem);
            Serializer.WriteItem(true, CurrentMapItem);

            AssertAndGoToStart(0, 0xD0);

            {
                Assert.AreEqual(true, Deserializer.ReadItem(CurrentMapItem));
                Assert.AreEqual(true, Deserializer.ReadItem(CurrentMapItem));
                Assert.AreEqual(false, Deserializer.ReadItem(CurrentMapItem));
                Assert.AreEqual(true, Deserializer.ReadItem(CurrentMapItem));
            }
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

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Byte(bool compressed)
        {
            Setup<byte>(compressed ? ABSaveSettings.ForSize : ABSaveSettings.ForSpeed);

            DoSerialize((byte)124);
            GoToStart();
            Assert.AreEqual(124, DoDeserialize<byte>());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void SByte(bool compressed)
        {
            Setup<sbyte>(compressed ? ABSaveSettings.ForSize : ABSaveSettings.ForSpeed);

            DoSerialize((sbyte)-5);
            GoToStart();
            Assert.AreEqual(-5, DoDeserialize<sbyte>());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Int16(bool compressed)
        {
            Setup<short>(compressed ? ABSaveSettings.ForSize : ABSaveSettings.ForSpeed);

            DoSerialize((short)1671);
            GoToStart();
            Assert.AreEqual(1671, DoDeserialize<short>());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void UInt16(bool compressed)
        {
            Setup<ushort>(compressed ? ABSaveSettings.ForSize : ABSaveSettings.ForSpeed);

            DoSerialize((ushort)1671);
            GoToStart();
            Assert.AreEqual(1671, DoDeserialize<ushort>());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Char(bool compressed)
        {
            Setup<char>(compressed ? ABSaveSettings.ForSize : ABSaveSettings.ForSpeed);

            DoSerialize('\u1056');
            GoToStart();
            Assert.AreEqual('\u1056', DoDeserialize<char>());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Int32(bool compressed)
        {
            Setup<int>(compressed ? ABSaveSettings.ForSize : ABSaveSettings.ForSpeed);

            DoSerialize(1671);
            GoToStart();
            Assert.AreEqual(1671, DoDeserialize<int>());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Int32_Zero(bool compressed)
        {
            Setup<int>(compressed ? ABSaveSettings.ForSize : ABSaveSettings.ForSpeed);

            DoSerialize(0);
            GoToStart();
            Assert.AreEqual(0, DoDeserialize<int>());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void UInt32(bool compressed)
        {
            Setup<uint>(compressed ? ABSaveSettings.ForSize : ABSaveSettings.ForSpeed);

            DoSerialize((uint)1671);
            GoToStart();
            Assert.AreEqual((uint)1671, DoDeserialize<uint>());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Int64(bool compressed)
        {
            Setup<long>(compressed ? ABSaveSettings.ForSize : ABSaveSettings.ForSpeed);

            DoSerialize(567L);
            GoToStart();
            Assert.AreEqual(567L, DoDeserialize<long>());
        }

        enum E
        {
            A,
            B,
            C
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Int32_Enum(bool compressed)
        {
            Setup<E>(compressed ? ABSaveSettings.ForSize : ABSaveSettings.ForSpeed);

            DoSerialize(E.A);
            GoToStart();
            Assert.AreEqual(E.A, DoDeserialize<E>());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void UInt64(bool compressed)
        {
            Setup<ulong>(compressed ? ABSaveSettings.ForSize : ABSaveSettings.ForSpeed);

            DoSerialize(567UL);
            GoToStart();
            Assert.AreEqual(567UL, DoDeserialize<ulong>());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Single(bool compressed)
        {
            Setup<float>(compressed ? ABSaveSettings.ForSize : ABSaveSettings.ForSpeed);

            DoSerialize(3.5f);
            GoToStart();
            Assert.AreEqual(3.5f, DoDeserialize<float>());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Double(bool compressed)
        {
            Setup<double>(compressed ? ABSaveSettings.ForSize : ABSaveSettings.ForSpeed);

            DoSerialize(3.5d);
            GoToStart();
            Assert.AreEqual(3.5d, DoDeserialize<double>());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Decimal(bool compressed)
        {
            Setup<decimal>(compressed ? ABSaveSettings.ForSize : ABSaveSettings.ForSpeed);

            DoSerialize(56.57M);
            GoToStart();
            Assert.AreEqual(56.57M, DoDeserialize<decimal>());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void IntPtr(bool compressed)
        {
            Assert.ThrowsException<Exception>(() => Setup<IntPtr>(compressed ? ABSaveSettings.ForSize : ABSaveSettings.ForSpeed));
        }
    }
}
