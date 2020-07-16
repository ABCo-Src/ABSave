using ABSoftware.ABSave.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
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
            InitWriter();
            for (int i = 0; i < 600; i++)
                _writer.WriteByte((byte)i);

            TestBytes(GenerateByteArr(600));
        }

        [TestMethod]
        public void WriteInt16()
        {
            InitWriter();
            _writer.WriteInt16(6);

            TestBytes(BitConverter.GetBytes((short)6));
        }

        [TestMethod]
        public void WriteInt16_Reverse()
        {
            InitWriter(new ABSaveSettings().SetUseLittleEndian(!BitConverter.IsLittleEndian));
            _writer.WriteInt16(6);

            TestBytes(BitConverter.GetBytes((short)6).Reverse().ToArray());
        }

        [TestMethod]
        public void WriteInt32()
        {
            InitWriter();
            _writer.WriteInt32(79643);

            TestBytes(BitConverter.GetBytes(79643));
        }

        [TestMethod]
        public void WriteInt32_Reverse()
        {
            InitWriter(new ABSaveSettings().SetUseLittleEndian(!BitConverter.IsLittleEndian));
            _writer.WriteInt32(79643);

            TestBytes(BitConverter.GetBytes(79643).Reverse().ToArray());
        }

        [TestMethod]
        public void WriteInt64()
        {
            InitWriter();
            _writer.WriteInt64(79643);

            TestBytes(BitConverter.GetBytes((long)79643));
        }

        [TestMethod]
        public void WriteInt64_Reverse()
        {
            InitWriter(new ABSaveSettings().SetUseLittleEndian(!BitConverter.IsLittleEndian));
            _writer.WriteInt64(79643);

            TestBytes(BitConverter.GetBytes((long)79643).Reverse().ToArray());
        }

        [TestMethod]
        public void WriteFloat()
        {
            InitWriter();
            _writer.WriteSingle(58.4f);

            TestBytes(BitConverter.GetBytes(58.4f));
        }

        [TestMethod]
        public void WriteFloat_Reverse()
        {
            InitWriter(new ABSaveSettings().SetUseLittleEndian(!BitConverter.IsLittleEndian));
            _writer.WriteSingle(58.4f);

            TestBytes(BitConverter.GetBytes(58.4f).Reverse().ToArray());
        }

        [TestMethod]
        public void WriteDouble()
        {
            InitWriter(new ABSaveSettings().SetUseLittleEndian(!BitConverter.IsLittleEndian));
            _writer.WriteDouble(58.4d);

            TestBytes(BitConverter.GetBytes(58.4d).Reverse().ToArray());
        }

        [TestMethod]
        public void WriteDouble_Reversed()
        {
            InitWriter(new ABSaveSettings().SetUseLittleEndian(!BitConverter.IsLittleEndian));
            _writer.WriteDouble(65.4d);

            TestBytes(BitConverter.GetBytes(65.4d).Reverse().ToArray());
        }

        [TestMethod]
        public void WriteByteArray_Small_NoSize()
        {
            InitWriter();
            var arr = GenerateByteArr(5);
            _writer.WriteByteArray(arr, false);

            TestBytes(arr);
        }

        [TestMethod]
        public void WriteByteArray_Large_NoSize()
        {
            InitWriter();
            var arr = GenerateByteArr(600);
            _writer.WriteByteArray(arr, false);

            TestBytes(arr);
        }

        [TestMethod]
        public void WriteText()
        {
            InitWriter();
            _writer.WriteText("ABC\u0001DEF");
            TestBytes(BitConverter.GetBytes(14).Concat(GetBytesOfShorts(true, 65, 66, 67, 1, 68, 69, 70)).ToArray());
        }

        [TestMethod]
        public void WriteText_Reversed()
        {
            InitWriter(new ABSaveSettings().SetUseLittleEndian(!BitConverter.IsLittleEndian));
            _writer.WriteText("ABC\u0001DEF");
            TestBytes(BitConverter.GetBytes(14).Reverse().Concat(GetBytesOfShorts(false, 65, 66, 67, 1, 68, 69, 70)).ToArray());
        }

        [TestMethod]
        public void Combo()
        {
            InitWriter();

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
            expected.AddRange(BitConverter.GetBytes(14));
            expected.AddRange(GetBytesOfShorts(true, 65, 66, 67, 1, 68, 69, 70));

            TestBytes(expected.ToArray());
        }

        #region Helpers
        public void InitWriter() => InitWriter(new ABSaveSettings());
        public void InitWriter(ABSaveSettings settings) => _writer = new ABSaveWriter(settings);

        public void TestBytes(byte[] expected)
        {
            var bArr = _writer.ToByteArray();
            for (int i = 0; i < bArr.Length; i++)
                if (expected[i] != bArr[i])
                    throw new Exception(i.ToString());
            CollectionAssert.AreEqual(expected, bArr);
        }

        public byte[] GenerateByteArr(int size)
        {
            var ret = new byte[size];
            for (int i = 0; i < size; i++)
                ret[i] = (byte)i;
            return ret;
        }

        public byte[] GetBytesOfShorts(bool matchSystemEndian, params short[] shorts)
        {
            var res = new byte[shorts.Length * 2];
            for (int i = 0; i < shorts.Length; i++)
            {
                var bytes = BitConverter.GetBytes(shorts[i]);

                if (!matchSystemEndian)
                    Array.Reverse(bytes);

                bytes.CopyTo(res, i * 2);
            }
            return res;
        }
        #endregion
    }
}
