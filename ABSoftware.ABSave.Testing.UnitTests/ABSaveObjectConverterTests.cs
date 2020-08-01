using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization;
using ABSoftware.ABSave.Serialization.Writer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Testing.UnitTests
{
    [TestClass]
    public class ABSaveObjectConverterTests
    {
        [TestMethod]
        public void Auto_SimpleStruct()
        {
            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            ABSaveObjectConverter.Serialize(new SimpleStruct(12), new TypeInformation(typeof(SimpleStruct), TypeCode.Object), actual);

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteInt32(1);
            expected.WriteText("Inside");
            expected.WriteInt32(12);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        public void Auto_SimpleObject()
        {
            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            ABSaveObjectConverter.Serialize(new SimpleClass(), new TypeInformation(typeof(SimpleClass), TypeCode.Object), actual);

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteInt32(3);
            expected.WriteText("Itm1");
            expected.WriteByte(1);
            expected.WriteText("Itm2");
            expected.WriteInt32(12);
            expected.WriteText("Itm3");
            ABSaveItemSerializer.SerializeAuto("abc", new TypeInformation(typeof(string), TypeCode.String), expected);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }
    }
}
