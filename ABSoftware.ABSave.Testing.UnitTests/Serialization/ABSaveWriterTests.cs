using ABSoftware.ABSave.Serialization;
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
        ABSaveWriter _writer;

        [TestMethod]
        public void WriteByte()
        {
            InitWriter(false);
            for (int i = 0; i < 600; i++)
                _writer.WriteByte((byte)i);

            TestBytes(GenerateByteArr(600));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void WriteInt16(bool reversed)
        {
            InitWriter(reversed);
            _writer.WriteInt16(6);

            TestBytes(GetBytes((short)6, reversed));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void WriteInt32(bool reversed)
        {
            InitWriter(reversed);
            _writer.WriteInt32(79643);

            TestBytes(GetBytes(79643, reversed));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void WriteInt64(bool reversed)
        {
            InitWriter(reversed);
            _writer.WriteInt64(79643);

            TestBytes(GetBytes((long)79643, reversed));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void WriteFloat(bool reversed)
        {
            InitWriter(reversed);
            _writer.WriteSingle(58.4f);

            TestBytes(GetBytes(58.4f, reversed));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void WriteDouble(bool reversed)
        {
            InitWriter(reversed);
            _writer.WriteDouble(58.4d);

            TestBytes(GetBytes(58.4d, reversed));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void WriteByteArray(bool reversed)
        {
            InitWriter(reversed);
            var arr = GenerateByteArr(600);
            _writer.WriteByteArray(arr, false);

            TestBytes(arr);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public unsafe void WriteText(bool reversed)
        {
            InitWriter(reversed);
            _writer.WriteText("ABC\u0001DEF");
            TestBytes(GetBytes(7, reversed).Concat(GetBytesOfShorts(reversed, 65, 66, 67, 1, 68, 69, 70)));
        }

        [TestMethod]
        public void WriteInt32ToSignificantBytes()
        {
            InitWriter(false);

            // 126721718 = B69E8D07
            // L ++-- --++ B
            const int NUMBER = 126721718;
            _writer.WriteLittleEndianInt32(NUMBER, 4);
            _writer.WriteLittleEndianInt32(NUMBER, 3);
            _writer.WriteLittleEndianInt32(NUMBER, 2);
            _writer.WriteLittleEndianInt32(NUMBER, 1);
            _writer.WriteLittleEndianInt32(NUMBER, 0);

            var expected = new byte[] { 0xB6, 0x9E, 0x8D, 0x07, 0xB6, 0x9E, 0x8D, 0xB6, 0x9E, 0xB6 };
            TestBytes(expected);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void WriteDecimal(bool reversed)
        {
            InitWriter(reversed);

            _writer.WriteDecimal(15092196.5M);

            var parts = decimal.GetBits(15092196.5M);
            var bits = new byte[16];
            for (int i = 0; i < 4; i++)
                Array.Copy(GetBytes(parts[i], reversed).ToArray(), 0, bits, i * 4, 4);

            TestBytes(bits);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Combo(bool reversed)
        {
            InitWriter(reversed);

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
            expected.AddRange(GetBytes(7, reversed));
            expected.AddRange(GetBytesOfShorts(reversed, 65, 66, 67, 1, 68, 69, 70));

            TestBytes(expected);
        }

        #region Helpers

        public void InitWriter(bool reversed) => InitWriter(new ABSaveSettings().SetUseLittleEndian(reversed ? !BitConverter.IsLittleEndian : BitConverter.IsLittleEndian));
        public void InitWriter(ABSaveSettings settings) => _writer = new ABSaveWriter(new MemoryStream(), settings);

        public void TestBytes(byte[] expected) => TestBytes(new List<byte>(expected));
        public void TestBytes(IEnumerable<byte> expected)
        {
            byte[] bArr = ((MemoryStream)_writer.Output).ToArray();

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
