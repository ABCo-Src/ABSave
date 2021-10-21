using ABCo.ABSave.Configuration;
using ABCo.ABSave.Serialization.Reading;
using ABCo.ABSave.Serialization.Writing;
using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Text;

namespace ABCo.ABSave.UnitTests.Core
{
    [TestClass]
    public class TextTests : TestBase
    {
        [TestMethod]
        public void String_NullableString_Null()
        {
            Initialize();

            Serializer.WriteNullableString(null);
            AssertAndGoToStart(0);

            Assert.AreEqual(null, Deserializer.ReadNullableString());
        }

        [TestMethod]
        public void String_NullableString_NotNull()
        {
            Initialize();

            Serializer.WriteNullableString("ABC");
            AssertAndGoToStart(0x83, (byte)'A', (byte)'B', (byte)'C');

            Assert.AreEqual("ABC", Deserializer.ReadNullableString());
        }

        [TestMethod]
        public void String_UTF8()
        {
            Initialize();

            Serializer.WriteNonNullString("ABC");
            AssertAndGoToStart(3, (byte)'A', (byte)'B', (byte)'C');

            Assert.AreEqual("ABC", Deserializer.ReadNonNullString());
        }

        [TestMethod]
        public void String_UTF16()
        {
            Initialize(ABSaveSettings.ForSpeed.Customize(c => c.SetUseUTF8(false)));

            // Small buffer
            Serializer.WriteNonNullString("ABC");
            AssertAndGoToStart(GetByteArr(new object[] { BitConverter.GetBytes('A'), BitConverter.GetBytes('B'), BitConverter.GetBytes('C') }, 3, (short)GenType.ByteArr, (short)GenType.ByteArr, (short)GenType.ByteArr));

            Assert.AreEqual("ABC", Deserializer.ReadNonNullString());

            GoToStart();

            // Large buffer
            string newStr = new string('J', 1200);
            Serializer.WriteNonNullString(newStr);
            AssertAndGoToStart(GetByteArr(new object[] { 1200UL, Encoding.Unicode.GetBytes(newStr) }, (short)GenType.Size, (short)GenType.ByteArr));

            Assert.AreEqual(newStr, Deserializer.ReadNonNullString());
        }

        [TestMethod]
        public void UTF8()
        {
            Initialize();

            // Stack buffer
            {
                Serializer.WriteUTF8("ABC".AsSpan());
                AssertAndGoToStart(3, (byte)'A', (byte)'B', (byte)'C');
            }

            {
                "ABC".AsSpan().SequenceEqual(Deserializer.ReadUTF8(s => new char[s], c => c.AsMemory()));
            }

            ResetState();

            // Heap buffer. (Trying twice to make sure getting an already used buffer works)
            TestHeapBuffer(1200);
            ResetState();
            TestHeapBuffer(1100);

            void TestHeapBuffer(int size)
            {
                var chArr = GenerateBlankCharArr(size);
                var expected = GenerateBlankExpected(size);

                {
                    Serializer.WriteUTF8(chArr.AsSpan());
                    AssertAndGoToStart(expected);
                }

                {
                    chArr.AsSpan().SequenceEqual(Deserializer.ReadUTF8(s => new char[s], c => c.AsMemory()));
                }
            }
        }

        static char[] GenerateBlankCharArr(int size)
        {
            var res = new char[size];
            Array.Fill(res, 'A');
            return res;
        }

        static byte[] GenerateBlankExpected(int size)
        {
            var expected = new byte[size + 2];
            expected[0] = 0b10000100;
            expected[1] = (byte)(size & 255);
            Array.Fill(expected, (byte)'A', 2, size);

            return expected;
        }
    }
}
