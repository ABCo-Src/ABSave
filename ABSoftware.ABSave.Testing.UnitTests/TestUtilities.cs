
using ABSoftware.ABSave.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ABSoftware.ABSave.Testing.UnitTests
{
    public static class TestUtilities
    {
        public static void CompareWriters(ABSaveSerializer expected, ABSaveSerializer actual)
        {
            CollectionAssert.AreEqual(((MemoryStream)expected.Output).ToArray(), ((MemoryStream)actual.Output).ToArray());
        }

        public static string RepeatString(string str, int count)
        {
            var res = string.Create(str.Length * count, str, new SpanAction<char, string>((dest, state) =>
            {
                int currentPos = 0;

                for (int i = 0; i < count; i++)
                    for (int j = 0; j < str.Length; j++)
                        dest[currentPos++] = str[j];
            }));

            return res;
        }
    }
}
