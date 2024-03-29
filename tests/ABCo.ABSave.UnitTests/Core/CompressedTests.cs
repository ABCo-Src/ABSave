﻿using ABCo.ABSave.Configuration;
using ABCo.ABSave.Serialization.Reading;
using ABCo.ABSave.Serialization.Writing;
using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ABCo.ABSave.UnitTests.Core
{
    [TestClass]
    public class CompressedTests : TestBase
    {
        // TODO: Add byte-by-byte testing for serialization. I did some work on this previously but it's just too much bit-by-bit work, 
        // I just can't keep track of the tests well enough to write them effectively. You can see what I initially wrote at the bottom.
        // For now it serializes then tests to see if deserialization succeeds. This works well enough to prove things are working.
        // If anyone wants to write full serialization tests, it would be hugely appreciated. - ABPerson

        [TestMethod]
        [DataRow(0)]
        [DataRow(-657)]
        [DataRow(471)]
        [DataRow(-26821087191097L)]
        [DataRow(26821087191100L)]
        public void Test8Free_Signed(long data) => Test((ulong)data, 8, true);

        [TestMethod]
        [DataRow(0)]
        [DataRow(-657)]
        [DataRow(471)]
        [DataRow(-26821087191097L)]
        [DataRow(26821087191100L)]
        public void Test7Free_Signed(long data) => Test((ulong)data, 7, true);

        [TestMethod]
        [DataRow(0)]
        [DataRow(-657)]
        [DataRow(471)]
        [DataRow(-26821087191097L)]
        [DataRow(26821087191100L)]
        public void Test6Free_Signed(long data) => Test((ulong)data, 6, true);

        [TestMethod]
        [DataRow(0)]
        [DataRow(-657)]
        [DataRow(471)]
        [DataRow(-26821087191097L)]
        [DataRow(26821087191100L)]
        public void Test5Free_Signed(long data) => Test((ulong)data, 5, true);

        [TestMethod]
        [DataRow(0)]
        [DataRow(-657)]
        [DataRow(471)]
        [DataRow(-26821087191097L)]
        [DataRow(26821087191100L)]
        public void Test4Free_Signed(long data) => Test((ulong)data, 4, true);

        [TestMethod]
        [DataRow(0)]
        [DataRow(-657)]
        [DataRow(471)]
        [DataRow(-26821087191097L)]
        [DataRow(26821087191100L)]
        public void Test3Free_Signed(long data) => Test((ulong)data, 3, true);

        [TestMethod]
        [DataRow(0)]
        [DataRow(-657)]
        [DataRow(471)]
        [DataRow(-26821087191097L)]
        [DataRow(26821087191100L)]
        public void Test2Free_Signed(long data) => Test((ulong)data, 2, true);

        [TestMethod]
        [DataRow(0)]
        [DataRow(-657)]
        [DataRow(471)]
        [DataRow(-26821087191097L)]
        [DataRow(26821087191100L)]
        public void Test1Free_Signed(long data) => Test((ulong)data, 1, true);

        [TestMethod]
        [DataRow(0LU)]
        [DataRow(8LU)]
        [DataRow(0b0001_0000_0001LU)]
        [DataRow(0b0100_0001_0001_0010LU)]
        [DataRow(0b0010_0110_0010_0101_1010_0000_0000LU)]
        [DataRow(0b1_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b1111_1101_0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b1_1111_1101_0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        public void Test8Free(ulong data) => Test(data, 8, false);

        [TestMethod]
        [DataRow(0LU)]
        [DataRow(8LU)]
        [DataRow(0b0001_0000_0001LU)]
        [DataRow(0b0100_0001_0001_0010LU)]
        [DataRow(0b0010_0110_0010_0101_1010_0000_0000LU)]
        [DataRow(0b1_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b1111_1101_0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b1_1111_1101_0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        public void Test7Free(ulong data) => Test(data, 7, false);

        [TestMethod]
        [DataRow(0LU)]
        [DataRow(8LU)]
        [DataRow(0b0001_0000_0001LU)]
        [DataRow(0b0100_0001_0001_0010LU)]
        [DataRow(0b0010_0110_0010_0101_1010_0000_0000LU)]
        [DataRow(0b1_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b1111_1101_0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b1_1111_1101_0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        public void Test6Free(ulong data) => Test(data, 6, false);

        [TestMethod]
        [DataRow(0LU)]
        [DataRow(8LU)]
        [DataRow(0b0001_0000_0001LU)]
        [DataRow(0b0100_0001_0001_0010LU)]
        [DataRow(0b0010_0110_0010_0101_1010_0000_0000LU)]
        [DataRow(0b1_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b1111_1101_0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b1_1111_1101_0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        public void Test5Free(ulong data) => Test(data, 5, false);

        [TestMethod]
        [DataRow(0LU)]
        [DataRow(8LU)]
        [DataRow(0b0001_0000_0001LU)]
        [DataRow(0b0100_0001_0001_0010LU)]
        [DataRow(0b0010_0110_0010_0101_1010_0000_0000LU)]
        [DataRow(0b1_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b1111_1101_0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b1_1111_1101_0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        public void Test4Free(ulong data) => Test(data, 4, false);

        [TestMethod]
        [DataRow(0LU)]
        [DataRow(8LU)]
        [DataRow(0b0001_0000_0001LU)]
        [DataRow(0b0100_0001_0001_0010LU)]
        [DataRow(0b0010_0110_0010_0101_1010_0000_0000LU)]
        [DataRow(0b1_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b1111_1101_0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b1_1111_1101_0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        public void Test3Free(ulong data) => Test(data, 3, false);

        [TestMethod]
        [DataRow(0LU)]
        [DataRow(8LU)]
        [DataRow(0b0001_0000_0001LU)]
        [DataRow(0b0100_0001_0001_0010LU)]
        [DataRow(0b0010_0110_0010_0101_1010_0000_0000LU)]
        [DataRow(0b1_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b1111_1101_0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b1_1111_1101_0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        public void Test2Free(ulong data) => Test(data, 2, false);

        [TestMethod]
        [DataRow(0LU)]
        [DataRow(8LU)]
        [DataRow(0b0001_0000_0001LU)]
        [DataRow(0b0100_0001_0001_0010LU)]
        [DataRow(0b0010_0110_0010_0101_1010_0000_0000LU)]
        [DataRow(0b1_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b1111_1101_0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        [DataRow(0b1_1111_1101_0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU)]
        public void Test1Free(ulong data) => Test(data, 1, false);

        void Test(ulong data, byte bitsFree, bool signed)
        {
            TestWithSettings(data, bitsFree, false, signed);
            TestWithSettings(data, bitsFree, true, signed);
        }

        void TestWithSettings(ulong data, byte bitsFree, bool lazy, bool signed)
        {
            Initialize(ABSaveSettings.ForSpeed, null, lazy);

            // Write up the number of bits we want free.
            Serializer.WriteInteger(0, (byte)(8 - bitsFree));

            bool isIntSized = !signed && data < uint.MaxValue || signed && (long)data <= int.MaxValue && (long)data >= int.MinValue;

            if (isIntSized)
            {
                if (signed)
                    Serializer.WriteCompressedIntSigned((int)data);
                else
                    Serializer.WriteCompressedInt((uint)data);
            }
            else
            {
                if (signed)
                    Serializer.WriteCompressedLongSigned((long)data);
                else
                    Serializer.WriteCompressedLong(data);
            }

            Serializer.WriteByte(127);
            GoToStart();

            // Deserialization
            {
                // Read up the number of bits we want free.
                Deserializer.ReadInteger((byte)(8 - bitsFree));

                if (isIntSized)
                {
                    Assert.AreEqual((uint)data, signed ? Deserializer.ReadCompressedIntSigned() : Deserializer.ReadCompressedInt());
                }
                else
                {
                    Assert.AreEqual(data, signed ? Deserializer.ReadCompressedLongSigned() : Deserializer.ReadCompressedLong());
                }
            }

            // Make sure we can still successfully read the byte after.
            Assert.AreEqual(127, Deserializer.ReadByte());
        }

        //[TestMethod]
        //[DataRow(8LU, new byte[] { 0b00001000 })]
        //[DataRow(0b0001_0000_0001LU, new byte[] { 0b10000001, 1 })]
        //[DataRow(0b0100_0001_0001_0010LU, new byte[] { 0b11000000, 0b01000001, 0b00010010 })]
        //[DataRow(0b0010_0110_0010_0101_1010_0000_0000LU, new byte[] { 0b11100010, 0b01100010, 0b01011010, 0 })]
        //[DataRow(0b1_0101_1001_1010_0101_0111_1111_1010_1010LU, new byte[] { 0b11110001, 0b01011001, 0b10100101, 0b01111111, 0b10101010 })]
        //// Long sizes:
        //[DataRow(0b1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU, new byte[] { 0b11111000, 0b10001110, 0b01011001, 0b10100101, 0b01111111, 0b10101010 })]
        //[DataRow(0b0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU, new byte[] { 0b11111100, 0b01011110, 0b10001110, 0b01011001, 0b10100101, 0b01111111, 0b10101010 })]
        //[DataRow(0b1111_1101_0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU, new byte[] { 0b11111110, 0b11111101, 0b01011110, 0b10001110, 0b01011001, 0b10100101, 0b01111111, 0b10101010 })]
        //[DataRow(0b1_1111_1101_0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU, new byte[] { 0b11111111, 0b00000001, 0b11111101, 0b01011110, 0b10001110, 0b01011001, 0b10100101, 0b01111111, 0b10101010 })]
        //public void With8Bits(ulong data) => Test(data, 8);

        //[TestMethod]
        //// Int sizes:
        //[DataRow(8LU, new byte[] { 0b0001000 })]
        //[DataRow(0b0001_0000_0001LU, new byte[] { 0b1000001, 1 })]
        //[DataRow(0b0100_0001_0001_0010LU, new byte[] { 0b1100000, 0b01000001, 0b00010010 })]
        //[DataRow(0b0010_0110_0010_0101_1010_0000_0000LU, new byte[] { 0b1110010, 0b01100010, 0b01011010, 0 })]
        //[DataRow(0b1_0101_1001_1010_0101_0111_1111_1010_1010LU, new byte[] { 0b1111001, 0b01011001, 0b10100101, 0b01111111, 0b10101010 })]
        //// Long sizes:
        //[DataRow(0b1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU, new byte[] { 0b1111100, 0b10001110, 0b01011001, 0b10100101, 0b01111111, 0b10101010 })]
        //[DataRow(0b0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU, new byte[] { 0b1111110, 0b01011110, 0b10001110, 0b01011001, 0b10100101, 0b01111111, 0b10101010 })]
        //[DataRow(0b1_1111_1101_0101_1110_1000_1110_0101_1001_1010_0101_0111_1111_1010_1010LU, new byte[] { 0b1111111, 0b10000001, 0b11111101, 0b01011110, 0b10001110, 0b01011001, 0b10100101, 0b01111111, 0b10101010 })]
        //public void With7Bits(ulong data) => Test(data, 7);
    }
}
