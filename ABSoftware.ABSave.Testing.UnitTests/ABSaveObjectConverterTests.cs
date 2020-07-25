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
        public ABSaveSettings NoNames = new ABSaveSettings().SetWithNames(false);
        public ABSaveSettings Names = new ABSaveSettings().SetWithNames(true);

        [TestMethod]
        public void Auto_NoNames_SimpleObject()
        {
            var actual = new ABSaveMemoryWriter(NoNames);
            ABSaveObjectConverter.AutoSerializeObject(new SimpleObject(), actual, new Helpers.TypeInformation(typeof(SimpleObject), TypeCode.Object));

            var expected = new ABSaveMemoryWriter(NoNames);
            expected.WriteInt32(3);
            expected.WriteByte(1);
            expected.WriteInt32(12);
            ABSaveItemSerializer.SerializeAuto("abc", expected, new Helpers.TypeInformation(typeof(string), TypeCode.String));

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        public void Auto_Names_SimpleObject()
        {
            var actual = new ABSaveMemoryWriter(Names);
            ABSaveObjectConverter.AutoSerializeObject(new SimpleObject(), actual, new Helpers.TypeInformation(typeof(SimpleObject), TypeCode.Object));

            var expected = new ABSaveMemoryWriter(Names);
            expected.WriteInt32(3);
            expected.WriteText("Itm1");
            expected.WriteByte(1);
            expected.WriteText("Itm2");
            expected.WriteInt32(12);
            expected.WriteText("Itm3");
            ABSaveItemSerializer.SerializeAuto("abc", expected, new Helpers.TypeInformation(typeof(string), TypeCode.String));

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }
    }
}
