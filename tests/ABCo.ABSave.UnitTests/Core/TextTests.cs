using ABCo.ABSave.Serialization.Reading;
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
                Serializer.WriteUTF8("ABC".AsSpan());
                AssertAndGoToStart(3, 65, 66, 67);
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
