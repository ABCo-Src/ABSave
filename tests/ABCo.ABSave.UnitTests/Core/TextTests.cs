using ABCo.ABSave.Serialization.Writing.Reading;
using ABCo.ABSave.Serialization.Writing;
using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace ABCo.ABSave.UnitTests.Core
{
    [TestClass]
    public class TextTests : TestBase
    {
        [TestMethod]
        public void String_UTF8()
        {
            Initialize();

            Serializer.WriteNonNullString("ABC");
            AssertAndGoToStart(3, 65, 66, 67);

            Assert.AreEqual("ABC", Deserializer.ReadNonNullString());
        }

        [TestMethod]
        public void UTF8()
        {
            Initialize();

            // Stack buffer
            {
                var header = Serializer.GetHeader();
                header.WriteUTF8("ABC".AsSpan());
                AssertAndGoToStart(3, 65, 66, 67);
            }

            {
                var header = Deserializer.GetHeader();
                "ABC".AsSpan().SequenceEqual(header.ReadUTF8(s => new char[s], c => c.AsMemory()));
            }

            ResetState();

            // Heap buffer. (Trying twice to make sure getting an already used buffer works)
            TestHeapBuffer();
            ResetState();
            TestHeapBuffer();

            void TestHeapBuffer()
            {
                var chArr = GenerateBlankCharArr();
                var expected = GenerateBlankExpected();

                {
                    var header = Serializer.GetHeader();
                    header.WriteUTF8(chArr.AsSpan());
                    AssertAndGoToStart(expected);
                }

                {
                    var header = Deserializer.GetHeader();
                    chArr.AsSpan().SequenceEqual(header.ReadUTF8(s => new char[s], c => c.AsMemory()));
                }
            }
        }

        static char[] GenerateBlankCharArr()
        {
            var res = new char[1200];
            Array.Fill(res, 'A');
            return res;
        }

        static byte[] GenerateBlankExpected()
        {
            var expected = new byte[1202];
            expected[0] = 0b10000100;
            expected[1] = 0b10110000;
            Array.Fill(expected, (byte)'A', 2, 1200);

            return expected;
        }
    }
}
