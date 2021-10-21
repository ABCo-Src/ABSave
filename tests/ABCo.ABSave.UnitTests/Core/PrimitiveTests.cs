using ABCo.ABSave.Configuration;
using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace ABCo.ABSave.UnitTests.Core
{
    [TestClass]
    public class PrimitiveTests : TestBase
    {
        [TestMethod]
        public void Byte()
        {
            Initialize();

            Serializer.WriteByte(5);
            Serializer.WriteByte(7);
            AssertAndGoToStart(5, 7);

            Assert.AreEqual(5, Deserializer.ReadByte());
            Assert.AreEqual(7, Deserializer.ReadByte());
        }

        [TestMethod]
        public void Bytes_Array()
        {
            Initialize();

            byte[] arr = new byte[4] { 1, 2, 3, 4 };
            Serializer.WriteRawBytes(arr);
            AssertAndGoToStart(1, 2, 3, 4);

            byte[] newArr = new byte[4];
            Deserializer.ReadBytes(newArr);

            CollectionAssert.AreEqual(arr, newArr);
        }

        [TestMethod]
        public void Bytes_Span()
        {
            Initialize();

            Span<byte> arr = new byte[4] { 1, 2, 3, 4 };
            Serializer.WriteRawBytes(arr);
            AssertAndGoToStart(1, 2, 3, 4);

            Span<byte> newArr = new byte[4];
            Deserializer.ReadBytes(newArr);

            CollectionAssert.AreEqual(arr.ToArray(), newArr.ToArray());
        }

        [TestMethod]
        public void FastShorts_KeepEndianness()
        {
            Initialize();

            Span<short> arr = new short[2] { 15, 26 };
            Serializer.FastWriteShorts(arr);
            AssertAndGoToStart(GetByteArr(
                new object[] { BitConverter.GetBytes((short)15), BitConverter.GetBytes((short)26) }, (short)GenType.ByteArr, (short)GenType.ByteArr));

            Span<short> newArr = new short[2];
            Deserializer.FastReadShorts(newArr);

            CollectionAssert.AreEqual(arr.ToArray(), newArr.ToArray());
        }

        [TestMethod]
        public void FastShorts_ReverseEndianness()
        {
            Initialize(ABSaveSettings.ForSpeed.Customize(c => c.SetUseLittleEndian(!BitConverter.IsLittleEndian)));

            Span<short> arr = new short[2] { 15, 26 };
            Serializer.FastWriteShorts(arr);
            AssertAndGoToStart(GetByteArr(
                new object[] { BitConverter.GetBytes((short)15).Reverse().ToArray(), BitConverter.GetBytes((short)26).Reverse().ToArray() }, (short)GenType.ByteArr, (short)GenType.ByteArr));

            Span<short> newArr = new short[2];
            Deserializer.FastReadShorts(newArr);

            CollectionAssert.AreEqual(arr.ToArray(), newArr.ToArray());
        }

        [TestMethod]
        public void GetStream_WithSurroundingBitSequence()
        {
            Initialize();

            Serializer.WriteBitOff();
            Serializer.WriteBitOn();
            Stream stream = Serializer.GetStream();
            stream.WriteByte(5);
            Serializer.WriteBitOn();
            Serializer.Flush();

            AssertAndGoToStart(0x40, 5, 0x80);

            Assert.IsFalse(Deserializer.ReadBit());
            Assert.IsTrue(Deserializer.ReadBit());

            Stream readStream = Deserializer.GetStream();
            Assert.AreEqual(5, readStream.ReadByte());

            Assert.IsTrue(Deserializer.ReadBit());
        }

        [DataRow(false)]
        [DataRow(true)]
        [TestMethod]
        public void Int16(bool reversed) => TestNum(d => Serializer.WriteInt16(d), () => Deserializer.ReadInt16(), (short)468, reversed);

        [DataRow(false)]
        [DataRow(true)]
        [TestMethod]
        public void Int32(bool reversed) => TestNum(d => Serializer.WriteInt32(d), () => Deserializer.ReadInt32(), 134217728, reversed);

        [DataRow(false)]
        [DataRow(true)]
        [TestMethod]
        public void Int64(bool reversed) => TestNum(d => Serializer.WriteInt64(d), () => Deserializer.ReadInt64(), long.MaxValue, reversed);

        [DataRow(false)]
        [DataRow(true)]
        [TestMethod]
        public void Single(bool reversed) => TestNum(d => Serializer.WriteSingle(d), () => Deserializer.ReadSingle(), float.MinValue, reversed);

        [DataRow(false)]
        [DataRow(true)]
        [TestMethod]
        public void Double(bool reversed) => TestNum(d => Serializer.WriteDouble(d), () => Deserializer.ReadDouble(), double.MaxValue, reversed);

        void TestNum(Action<dynamic> write, Func<dynamic> read, dynamic val, bool reversed)
        {
            if (reversed)
            {
                Initialize(ABSaveSettings.ForSpeed.Customize(b => b.SetUseLittleEndian(!BitConverter.IsLittleEndian)));

                write(val);
                AssertAndGoToStart(((byte[])BitConverter.GetBytes(val)).Reverse().ToArray());

                Assert.AreEqual(val, read());
            }
            else
            {
                Initialize();

                write(val);
                AssertAndGoToStart((byte[])BitConverter.GetBytes(val));

                Assert.AreEqual(val, read());
            }
        }
    }
}
