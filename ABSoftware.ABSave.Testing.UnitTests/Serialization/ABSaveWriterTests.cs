using ABSoftware.ABSave.Serialization;
using ABSoftware.ABSave.Serialization.Writer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace ABSoftware.ABSave.Testing.UnitTests.Serialization
{
    [TestClass]
    public class ABSaveWriterTests
    {
        bool _isMemory;
        MemoryStream _nonMemoryStream;
        ABSaveWriter _writer;

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void WriteByte(bool isMemory)
        {
            InitWriter(isMemory, false);
            for (int i = 0; i < 600; i++)
                _writer.WriteByte((byte)i);

            TestBytes(GenerateByteArr(600));
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public void WriteInt16(bool isMemory, bool reversed)
        {
            InitWriter(isMemory, reversed);
            _writer.WriteInt16(6);

            TestBytes(GetBytes((short)6, reversed));
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public void WriteInt32(bool isMemory, bool reversed)
        {
            InitWriter(isMemory, reversed);
            _writer.WriteInt32(79643);

            TestBytes(GetBytes(79643, reversed));
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public void WriteInt64(bool isMemory, bool reversed)
        {
            InitWriter(isMemory, reversed);
            _writer.WriteInt64(79643);

            TestBytes(GetBytes((long)79643, reversed));
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public void WriteFloat(bool isMemory, bool reversed)
        {
            InitWriter(isMemory, reversed);
            _writer.WriteSingle(58.4f);

            TestBytes(GetBytes(58.4f, reversed));
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public void WriteDouble(bool isMemory, bool reversed)
        {
            InitWriter(isMemory, reversed);
            _writer.WriteDouble(58.4d);

            TestBytes(GetBytes(58.4d, reversed));
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public void WriteByteArray_Small_NoSize(bool isMemory, bool reversed)
        {
            InitWriter(isMemory, reversed);
            var arr = GenerateByteArr(5);
            _writer.WriteByteArray(arr, false);

            TestBytes(arr);
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public void WriteByteArray_Large_NoSize(bool isMemory, bool reversed)
        {
            InitWriter(isMemory, reversed);
            var arr = GenerateByteArr(600);
            _writer.WriteByteArray(arr, false);

            TestBytes(arr);
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public void WriteByteArray_Small(bool isMemory, bool reversed)
        {
            InitWriter(isMemory, reversed);
            var arr = GenerateByteArr(5);
            _writer.WriteByteArray(arr, true);

            TestBytes(GetBytes(5, reversed).Concat(arr));
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public void WriteByteArray_Large(bool isMemory, bool reversed)
        {
            InitWriter(isMemory, reversed);
            var arr = GenerateByteArr(600);
            _writer.WriteByteArray(arr, true);

            TestBytes(GetBytes(600, reversed).Concat(arr));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void WriteByteArrayMemory_Large_NeedsDedicatedSizeChunk(bool reversed)
        {
            InitWriter(true, reversed);
            var arr = GenerateByteArr(65535);

            var memWriter = _writer as ABSaveMemoryWriter;
            memWriter.FreeSpace = 1;
            memWriter.CurrentChunk = memWriter.DataStart = memWriter.DataEnd = new LinkedMemoryDataChunk(1);

            _writer.WriteByteArray(arr, true);

            TestBytes(GetBytes(65535, reversed).Concat(arr));
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public unsafe void WriteText_Small(bool isMemory, bool reversed)
        {
            InitWriter(isMemory, reversed);
            _writer.WriteText("ABC\u0001DEF");
            TestBytes(GetBytes(14, reversed).Concat(GetBytesOfShorts(reversed, 65, 66, 67, 1, 68, 69, 70)));
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public unsafe void WriteText_Large(bool isMemory, bool reversed)
        {
            InitWriter(isMemory, reversed);

            _writer.WriteText(RepeatString("ABC\u0001DEF", 100));
            var stringAsShorts = RepeatBytes(GetBytesOfShorts(reversed, 65, 66, 67, 1, 68, 69, 70), 100);
            TestBytes(GetBytes(1400, reversed).Concat(stringAsShorts));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public unsafe void WriteTextMemory_Large_CharAcrossTwoChunks(bool reversed)
        {
            InitWriter(true, reversed);

            var memWriter = _writer as ABSaveMemoryWriter;
            memWriter.FreeSpace = 55;
            memWriter.CurrentChunk = memWriter.DataStart = memWriter.DataEnd = new LinkedMemoryDataChunk(55);

            _writer.WriteText(RepeatString("ABC\u0001DEF", 100));
            var stringAsShorts = RepeatBytes(GetBytesOfShorts(reversed, 65, 66, 67, 1, 68, 69, 70), 100);
            TestBytes(GetBytes(1400, reversed).Concat(stringAsShorts));
        }


        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public void WriteInt32ToSignificantBytes(bool isMemory, bool reversed)
        {
            InitWriter(isMemory, reversed);

            // 126721718 = 078D9EB6
            // L ++-- --++ B
            const int NUMBER = 126721718;
            _writer.WriteInt32ToSignificantBytes(NUMBER, 4);
            _writer.WriteInt32ToSignificantBytes(NUMBER, 3);
            _writer.WriteInt32ToSignificantBytes(NUMBER, 2);
            _writer.WriteInt32ToSignificantBytes(NUMBER, 1);
            _writer.WriteInt32ToSignificantBytes(NUMBER, 0);

            var expected = new List<byte>() { };

            void AddToExpected(byte[] toWrite)
            {
                if (BitConverter.IsLittleEndian) expected.AddRange(reversed ? toWrite : toWrite.Reverse());
                else expected.AddRange(reversed ? toWrite.Reverse() : toWrite);
            }

            expected.AddRange(GetBytes(NUMBER, reversed));
            AddToExpected(new byte[] { 0x8D, 0x9E, 0xB6 });
            AddToExpected(new byte[] { 0x9E, 0xB6 });
            AddToExpected(new byte[] { 0xB6 });

            TestBytes(expected);
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public void WriteDecimal(bool isMemory, bool reversed)
        {
            InitWriter(isMemory, reversed);

            _writer.WriteDecimal(15092196.5M);

            var parts = decimal.GetBits(15092196.5M);
            var bits = new byte[16];
            for (int i = 0; i < 4; i++)
                Array.Copy(GetBytes(parts[i], reversed).ToArray(), 0, bits, i * 4, 4);

            TestBytes(bits);
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public void Combo(bool isMemory, bool reversed)
        {
            InitWriter(isMemory, reversed);

            var arr = GenerateByteArr(600);

            _writer.WriteByte(9);
            _writer.WriteByte(48);
            _writer.WriteByteArray(arr, false);
            _writer.WriteText("ABC\u0001DEF");

            var expected = new List<byte>()
            {
                9, 48
            };

            expected.AddRange(arr);
            expected.AddRange(GetBytes(14, reversed));
            expected.AddRange(GetBytesOfShorts(reversed, 65, 66, 67, 1, 68, 69, 70));

            TestBytes(expected);
        }

        #region Helpers

        public void InitWriter(bool isMemory, bool reversed) => InitWriter(new ABSaveSettings().SetUseLittleEndian(reversed ? !BitConverter.IsLittleEndian : BitConverter.IsLittleEndian), isMemory);
        public void InitWriter(ABSaveSettings settings, bool isMemory)
        {
            _isMemory = isMemory;

            if (isMemory)
                _writer = new ABSaveMemoryWriter(settings);
            else
            {
                _nonMemoryStream = new MemoryStream();
                _writer = new ABSaveStreamWriter(_nonMemoryStream, settings);
            }
        }

        public void TestBytes(byte[] expected) => TestBytes(new List<byte>(expected));
        public void TestBytes(IEnumerable<byte> expected)
        {
            byte[] bArr = _isMemory ? ((ABSaveMemoryWriter)_writer).ToBytes() : _nonMemoryStream.ToArray();

            int i = 0;
            foreach (byte itm in expected)
                if (bArr[i++] != itm)
                    throw new Exception((i - 1).ToString());
        }

        public byte[] GenerateByteArr(int size)
        {
            var ret = new byte[size];
            for (int i = 0; i < size; i++)
                ret[i] = (byte)i;
            return ret;
        }

        public byte[] GetBytesOfShorts(bool reverseBytes, params short[] shorts)
        {
            var res = new byte[shorts.Length * 2];
            for (int i = 0; i < shorts.Length; i++)
            {
                var bytes = BitConverter.GetBytes(shorts[i]);

                if (reverseBytes)
                    Array.Reverse(bytes);

                bytes.CopyTo(res, i * 2);
            }
            return res;
        }

        public string RepeatString(string str, int noIterations)
        {
            char[] ch = new char[str.Length * noIterations];

            for (int i = 0; i < noIterations; i++)
                str.CopyTo(0, ch, i * str.Length, str.Length);

            return new string(ch);
        }

        public byte[] RepeatBytes(byte[] shorts, int noIterations)
        {
            byte[] res = new byte[shorts.Length * noIterations];

            for (int i = 0; i < noIterations; i++)
                shorts.CopyTo(res, i * shorts.Length);

            return res;
        }

        public IEnumerable<byte> GetBytes(dynamic obj, bool reverse)
        {
            if (reverse)
                return ((IEnumerable<byte>)BitConverter.GetBytes(obj)).Reverse();
            else
                return BitConverter.GetBytes(obj);
        }
        #endregion
    }
}
