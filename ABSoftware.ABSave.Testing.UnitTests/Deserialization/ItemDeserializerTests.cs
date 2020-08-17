using ABSoftware.ABSave.Deserialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ABSoftware.ABSave.Testing.UnitTests.Deserialization
{
    [TestClass]
    public class ItemDeserializerTests
    {
        [TestMethod]
        public void Attributes_Null()
        {
            var reader = new ABSaveReader(new MemoryStream(new byte[] { 1 } ), new ABSaveSettings());
            Assert.IsNull(ABSaveItemConverter.DeserializeAttribute(reader, typeof(object)));
        }

        [TestMethod]
        public void Attributes_MatchingItem()
        {
            var reader = new ABSaveReader(new MemoryStream(new byte[] { 2 }), new ABSaveSettings());
            Assert.AreEqual(typeof(object), ABSaveItemConverter.DeserializeAttribute(reader, typeof(object)));
        }
    }
}
