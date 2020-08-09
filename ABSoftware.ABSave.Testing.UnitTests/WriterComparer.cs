using ABSoftware.ABSave.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ABSoftware.ABSave.Testing.UnitTests
{
    public static class WriterComparer
    {
        public static void Compare(ABSaveWriter expected, ABSaveWriter actual)
        {
            CollectionAssert.AreEqual(((MemoryStream)expected.Output).ToArray(), ((MemoryStream)actual.Output).ToArray());
        }
    }
}
