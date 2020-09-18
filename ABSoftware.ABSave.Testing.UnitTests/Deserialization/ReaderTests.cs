using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ABSoftware.ABSave.Testing.UnitTests.Deserialization
{
    [TestClass]
    public class ReaderTests
    {
        ABSaveReader _reader;

        [TestMethod]
        public void ReadByte()
        {
            InitReader(new byte[] { 26 }, false);

            byte b = _reader.ReadByte();
            Assert.AreEqual(26, b);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ReadBytes(bool isHeap)
        {
            var src = new byte[] { 65, 66, 67 };
            InitReader(src, false);

            if (isHeap)
            {
                byte[] destHeap = new byte[3];
                _reader.ReadBytes(destHeap);
                CollectionAssert.AreEqual(src, destHeap);
            } 
            else
            {
                Span<byte> destStack = stackalloc byte[3];
                _reader.ReadBytes(destStack);

                Assert.AreEqual(65, destStack[0]);
                Assert.AreEqual(66, destStack[1]);
                Assert.AreEqual(67, destStack[2]);
            }
        }

        [TestMethod]
        public void ReadUTF8_NullTerminated()
        {
            var src = new List<byte>
            {
                65,
                66,
                67,
                0
            };

            ReadTextAndAssert("ABC", src.ToArray(), TextMode.NullTerminatedUTF8);
        }

        [TestMethod]
        public void ReadUTF8_NullTerminated_Large()
        {
            var src = new List<byte>();
            
            for (int i = 0; i < 200; i++)
            {
                src.Add(65);
                src.Add(66);
                src.Add(67);
                src.Add(68);
            }
            src.Add(0);

            ReadTextAndAssert(TestUtilities.RepeatString("ABCD", 200), src.ToArray(), TextMode.NullTerminatedUTF8);
        }

        [TestMethod]
        public void ReadUTF8_NullTerminated_ContainsNullCharacters()
        {
            var src = new List<byte>
            {
                65,
                66,
                0xC0, 
                0x80,
                67,
                0
            };

            ReadTextAndAssert("AB\0C", src.ToArray(), TextMode.NullTerminatedUTF8);
        }

        [TestMethod]
        public void ReadUTF8()
        {
            var src = new List<byte>();
            AddInt32(src, 3, false);
            src.Add(65);
            src.Add(66);
            src.Add(67);

            ReadTextAndAssert("ABC", src.ToArray(), TextMode.UTF8);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ReadUTF16(bool reversed)
        {
            var src = new List<byte>();
            AddInt32(src, 3, reversed);
            AddChar(src, 'A', reversed);
            AddChar(src, 'B', reversed);
            AddChar(src, 'C', reversed);

            ReadTextAndAssert("ABC", src.ToArray(), TextMode.UTF16, reversed);
        }

        void ReadTextAndAssert(string expectedStr, byte[] src, TextMode textMode, bool reversed = false)
        {
            var expected = expectedStr.ToCharArray();

            var settings = new ABSaveSettings().SetTextMode(textMode).SetUseLittleEndian(reversed ? !BitConverter.IsLittleEndian : BitConverter.IsLittleEndian);
            InitReader(src, settings);
            var str = _reader.ReadString();

            InitReader(src, settings);
            var chArr = _reader.ReadCharArr();

            CollectionAssert.AreEqual(expected, str.ToCharArray());
            CollectionAssert.AreEqual(expected, chArr);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ReadInt16(bool reversed)
        {
            InitReader(GetBytes((short)6, reversed), reversed);
            Assert.AreEqual(6, _reader.ReadInt16());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ReadInt32(bool reversed)
        {
            InitReader(GetBytes(16178, reversed), reversed);
            Assert.AreEqual((uint)16178, _reader.ReadInt32());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ReadInt64(bool reversed)
        {
            InitReader(GetBytes((long)16171791, reversed), reversed);
            Assert.AreEqual((ulong)16171791, _reader.ReadInt64());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ReadSingle(bool reversed)
        {
            InitReader(GetBytes(12.4f, reversed), reversed);
            Assert.AreEqual(12.4f, _reader.ReadSingle());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ReadDouble(bool reversed)
        {
            InitReader(GetBytes(12.4215d, reversed), reversed);
            Assert.AreEqual(12.4215d, _reader.ReadDouble());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ReadDecimal(bool reversed)
        {
            var parts = decimal.GetBits(15092196.5M);
            var bits = new byte[16];
            for (int i = 0; i < 4; i++)
                Array.Copy(GetBytes(parts[i], reversed).ToArray(), 0, bits, i * 4, 4);

            InitReader(bits, reversed);

            Assert.AreEqual(15092196.5M, _reader.ReadDecimal());
        }

        [TestMethod]
        public void ReadInt32ToSignificantBytes()
        {
            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());

            writer.WriteLittleEndianInt32(0xFFFFFFFF, 4);
            writer.WriteLittleEndianInt32(0xFFFFFF, 3);
            writer.WriteLittleEndianInt32(0xFFFF, 2);
            writer.WriteLittleEndianInt32(0xFF, 1);
            writer.WriteLittleEndianInt32(0, 0);

            InitReader(memoryStream.ToArray(), new ABSaveSettings());

            Assert.AreEqual(0xFFFFFFFF, _reader.ReadLittleEndianInt32(4));
            Assert.AreEqual((uint)0xFFFFFF, _reader.ReadLittleEndianInt32(3));
            Assert.AreEqual((uint)0xFFFF, _reader.ReadLittleEndianInt32(2));
            Assert.AreEqual((uint)0xFF, _reader.ReadLittleEndianInt32(1));
            Assert.AreEqual((uint)0, _reader.ReadLittleEndianInt32(0));
        }

        #region Helpers

        public void InitReader(byte[] contents, bool reversed) => InitReader(contents, new ABSaveSettings().SetUseLittleEndian(reversed ? !BitConverter.IsLittleEndian : BitConverter.IsLittleEndian));
        public void InitReader(byte[] contents, ABSaveSettings settings) => _reader = new ABSaveReader(new MemoryStream(contents), settings);

        public void AddChar(List<byte> dest, char ch, bool reversed)
        {
            var bits = BitConverter.GetBytes(ch);
            dest.AddRange(reversed ? bits.Reverse() : bits);
        }

        public void AddInt32(List<byte> dest, int integer, bool reversed)
        {
            var bits = BitConverter.GetBytes(integer);
            dest.AddRange(reversed ? bits.Reverse() : bits);
        }

        public byte[] GetBytes(dynamic itm, bool reversed)
        {
            return reversed ? Enumerable.ToArray(Enumerable.Reverse(BitConverter.GetBytes(itm))) : BitConverter.GetBytes(itm);
        }

        #endregion
    }
}
