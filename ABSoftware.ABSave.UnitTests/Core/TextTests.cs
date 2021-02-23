using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Serialization;
using ABSoftware.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABSoftware.ABSave.UnitTests.Core
{
    [TestClass]
    public class TextTests : TestBase
    {
        [TestMethod]
        public void String_UTF8()
        {
            Initialize();

            Serializer.WriteString("ABC");
            AssertAndGoToStart(3, 65, 66, 67);

            Assert.AreEqual("ABC", Deserializer.ReadString());
        }

        [TestMethod]
        public void UTF8()
        {
            Initialize();

            // Stack buffer
            {
                var header = new BitTarget(Serializer);
                Serializer.WriteUTF8("ABC".AsSpan(), ref header);
                AssertAndGoToStart(3, 65, 66, 67);
            }

            {
                var header = new BitSource(Deserializer);
                "ABC".AsSpan().SequenceEqual(Deserializer.ReadUTF8(s => new char[s], c => c.AsMemory(), ref header));
            }

            ResetOutput();

            // Heap buffer. (Trying twice to make sure getting an already used buffer works)
            TestHeapBuffer();
            ResetOutput();
            TestHeapBuffer();

            void TestHeapBuffer()
            {
                var chArr = GenerateBlankCharArr();
                var expected = GenerateBlankExpected();

                {
                    var header = new BitTarget(Serializer);
                    Serializer.WriteUTF8(chArr.AsSpan(), ref header);
                    AssertAndGoToStart(expected);
                }

                {
                    var header = new BitSource(Deserializer);
                    chArr.AsSpan().SequenceEqual(Deserializer.ReadUTF8(s => new char[s], c => c.AsMemory(), ref header));
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
