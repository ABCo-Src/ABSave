using ABCo.ABSave.Configuration;
using ABCo.ABSave.Serialization.Converters;
using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace ABCo.ABSave.UnitTests.Converters
{
    [TestClass]
    public class ArrayTests : ConverterTestBase
    {
        static ABSaveSettings Settings = null!;

        [TestInitialize]
        public void SetupSettings()
        {
            Settings = ABSaveSettings.ForSpeed;
        }

        [TestMethod]
        [DataRow(false)]
        public void SZSlow(bool unknown)
        {
            var arr = new string[] { "A", "B", "C", "D", "E" };

            if (unknown)
            {
                //Setup<Array>(Settings);
                //DoSerialize(arr);

                //Action<ABSaveSerializer> getElemType = s => s.WriteClosedType(typeof(string));

                //AssertAndGoToStart(GetByteArr(new object[] { getElemType }, (short)GenType.Action, 5, 193, 65, 193, 66, 193, 67, 193, 68, 193, 69));
                //CollectionAssert.AreEqual(arr, DoDeserialize<Array>());
            }
            else
            {
                Setup<string[]>(Settings);

                DoSerialize(arr);
                AssertAndGoToStart(0, 5, 0x80, 1, 65, 0x81, 66, 0x81, 67, 0x81, 68, 0x81, 69);
                CollectionAssert.AreEqual(arr, DoDeserialize<string[]>());
            }
        }

        [TestMethod]
        [DataRow(false)]
        public void SZFast_Byte(bool unknown)
        {
            Setup<byte[]>(Settings);

            var arr = new byte[] { 2, 7, 167 };

            DoSerialize(arr);
            AssertAndGoToStart(0, 3, 0, 2, 7, 167);
            CollectionAssert.AreEqual(arr, DoDeserialize<byte[]>());
        }

        [TestMethod]
        [DataRow(false)]
        public void SZFast_SByte(bool unknown)
        {
            Setup<sbyte[]>(Settings);

            var arr = new sbyte[] { -56, 13, -9 };

            DoSerialize(arr);
            AssertAndGoToStart(0, 3, 0, unchecked((byte)-56), 13, unchecked((byte)-9));
            CollectionAssert.AreEqual(arr, DoDeserialize<sbyte[]>());
        }

        [TestMethod]
        [DataRow(false)]
        public void SZFast_Int16(bool unknown) => 
            TestFastArray(new short[] { 125, -65, 31553 });

        [TestMethod]
        [DataRow(false)]
        public void SZFast_UInt16(bool unknown) => 
            TestFastArray(new ushort[] { 0, 34567, 62145 });

        [TestMethod]
        [DataRow(false)]
        public void SZFast_Int32(bool unknown) =>
            TestFastArray(new int[] { 34567, 62145, 2136617717 });

        [TestMethod]
        [DataRow(false)]
        public void SZFast_UInt32(bool unknown) =>
            TestFastArray(new uint[] { 34567U, 62145U, 4136617717U });

        [TestMethod]
        [DataRow(false)]
        public void SZFast_Int64(bool unknown) =>
            TestFastArray(new long[] { 71710L, 125674L, 5336617717L });

        [TestMethod]
        [DataRow(false)]
        public void SZFast_UInt64(bool unknown) =>
            TestFastArray(new ulong[] { 71710UL, 125674UL, ulong.MaxValue });

        [TestMethod]
        [DataRow(false)]
        public void SZFast_Single(bool unknown) =>
            TestFastArray(new float[] { 6.5f, 7681217f, 15171f });

        [TestMethod]
        [DataRow(false)]
        public void SZFast_Double(bool unknown) =>
            TestFastArray(new double[] { 6.5D, 7681217D, 15171D });

        void TestFastArray<T>(T[] arr)
        {
            Setup<T[]>(Settings);

            var expectedData = GetByteArr(
                new object[] { BitConverter.GetBytes((dynamic)arr[0]), BitConverter.GetBytes((dynamic)arr[1]), BitConverter.GetBytes((dynamic)arr[2]) }, 
                (short)GenType.ByteArr, (short)GenType.ByteArr, (short)GenType.ByteArr);

            var expectedReversedData = GetByteArr(
                new object[] { Reverse(BitConverter.GetBytes((dynamic)arr[0])), Reverse(BitConverter.GetBytes((dynamic)arr[1])), Reverse(BitConverter.GetBytes((dynamic)arr[2])) },
                (short)GenType.ByteArr, (short)GenType.ByteArr, (short)GenType.ByteArr);
            
            // Normal:
            TestKeepAndReverse(0, 3, 0);

            // Without version:
            Setup<T[]>(CurrentMap.Settings, null, false);

            Serializer = CurrentMap.GetSerializer(Stream, false);
            Deserializer = CurrentMap.GetDeserializer(Stream, false);
            TestKeepAndReverse(3);

            // With compressed:
            Setup<T[]>(ABSaveSettings.ForSize);

            var expectedCompressedData = GetByteArr(new object[] { arr[0], arr[1], arr[2] }, (short)GenType.Size, (short)GenType.Size, (short)GenType.Size);

            DoSerialize(arr);
            AssertAndGoToStart(GetByteArr(new object[] { expectedCompressedData }, 0, 3, 0, (short)GenType.ByteArr));
            CollectionAssert.AreEqual(arr, DoDeserialize<T[]>());

            void TestKeepAndReverse(params byte[] arrHeader)
            {
                // Keep endianness
                DoSerialize(arr);
                AssertAndGoToStart(GetByteArr(new object[] { arrHeader, expectedData }, (short)GenType.ByteArr, (short)GenType.ByteArr));
                CollectionAssert.AreEqual(arr, DoDeserialize<T[]>());

                ResetState();

                // Reverse endianness
                var before = CurrentMap.Settings;
                Setup<T[]>(CurrentMap.Settings.Customize(b => b.SetUseLittleEndian(!BitConverter.IsLittleEndian)), null, Serializer.State.IncludeVersioningInfo);

                DoSerialize(arr);
                AssertAndGoToStart(GetByteArr(new object[] { arrHeader, expectedReversedData }, (short)GenType.ByteArr, (short)GenType.ByteArr));
                CollectionAssert.AreEqual(arr, DoDeserialize<T[]>());

                Setup<T[]>(before);
            }

            Assert.IsTrue(((ArrayConverter)CurrentMapItem.Converter).IsSerializingFast);
        }

        [TestMethod]
        [DataRow(false)]
        public void SNZ(bool unknown)
        {
            var arr = Array.CreateInstance(typeof(byte), new int[] { 5 }, new int[] { 2 });
            arr.SetValue((byte)2, 2);
            arr.SetValue((byte)7, 3);
            arr.SetValue((byte)167, 4);
            arr.SetValue((byte)43, 5);
            arr.SetValue((byte)32, 6);

            if (unknown)
            {
                //Setup<Array>(Settings);

                //DoSerialize(arr);

                //Action<ABSaveSerializer> getElemType = s => s.WriteClosedType(typeof(byte));

                //AssertAndGoToStart(GetByteArr(new object[] { getElemType }, (short)GenType.Action, 69, 2, 2, 7, 167, 43, 32));
                //CollectionAssert.AreEqual(arr, DoDeserialize<Array>());
            }
            else
            {
                // Setup<Int32[*]>
                Setup(ABSaveSettings.ForSize, typeof(string).Assembly.GetType("System.Byte[*]"));

                DoSerialize(arr);
                AssertAndGoToStart(0, 5, 2, 0, 2, 7, 167, 43, 32);
                CollectionAssert.AreEqual(arr, DoDeserialize<ICollection>());
            }
        }

        [TestMethod]
        [DataRow(false)]
        public void MD_ZeroLowerBounds(bool unknown)
        {
            var arr = Array.CreateInstance(typeof(byte), new int[] { 2, 3, 2 });
            arr.SetValue((byte)2, 0, 0, 0);
            arr.SetValue((byte)7, 0, 0, 1);
            arr.SetValue((byte)167, 0, 1, 0);
            arr.SetValue((byte)43, 0, 1, 1);
            arr.SetValue((byte)32, 0, 2, 0);
            arr.SetValue((byte)54, 0, 2, 1);
            arr.SetValue((byte)67, 1, 0, 0);
            arr.SetValue((byte)68, 1, 0, 1);
            arr.SetValue((byte)69, 1, 1, 0);
            arr.SetValue((byte)70, 1, 1, 1);
            arr.SetValue((byte)71, 1, 2, 0);
            arr.SetValue((byte)72, 1, 2, 1);

            if (unknown)
            {
                //Setup<Array>(Settings);

                //DoSerialize(arr);

                //Action<ABSaveSerializer> getElemType = s => s.WriteClosedType(typeof(byte));

                //AssertAndGoToStart(GetByteArr(new object[] { getElemType }, (short)GenType.Action, 134, 2, 3, 2, 2, 7, 167, 43, 32, 54, 67, 68, 69, 70, 71, 72));
                //CollectionAssert.AreEqual(arr, DoDeserialize<Array>());
            }
            else
            {
                Setup<byte[,,]>(Settings);

                DoSerialize(arr);
                AssertAndGoToStart(0, 2, 3, 2, 0, 2, 7, 167, 43, 32, 54, 67, 68, 69, 70, 71, 72);
                CollectionAssert.AreEqual(arr, DoDeserialize<byte[,,]>());
            }
        }

        [TestMethod]
        [DataRow(false)]
        public void MD_LowerBounds(bool unknown)
        {
            var arr = Array.CreateInstance(typeof(byte), new int[] { 2, 2 }, new int[] { 9, 6 });
            arr.SetValue((byte)2, 9, 6);
            arr.SetValue((byte)7, 9, 7);
            arr.SetValue((byte)167, 10, 6);
            arr.SetValue((byte)43, 10, 7);

            if (unknown)
            {
                //Setup<Array>(Settings);

                //DoSerialize(arr);

                //Action<ABSaveSerializer> getElemType = s => s.WriteClosedType(typeof(byte));

                //AssertAndGoToStart(GetByteArr(new object[] { getElemType }, (short)GenType.Action, 196, 2, 2, 9, 6, 2, 7, 167, 43));
                //CollectionAssert.AreEqual(arr, DoDeserialize<Array>());
            }
            else
            {
                Setup<byte[,]>(Settings);

                DoSerialize(arr);
                AssertAndGoToStart(0, 130, 2, 9, 6, 0, 2, 7, 167, 43);
                CollectionAssert.AreEqual(arr, DoDeserialize<byte[,]>());
            }
        }
    }
}
