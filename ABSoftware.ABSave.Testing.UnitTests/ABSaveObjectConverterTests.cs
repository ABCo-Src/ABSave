using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping;
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
        public void Serialize_SimpleStruct()
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
        public void Serialize_SimpleObject()
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
            ABSaveItemSerializer.Serialize("abc", new TypeInformation(typeof(string), TypeCode.String), expected);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        public void Serialize_SimpleObject_MapReflection()
        {
            var map = new ObjectMapItem(3)
                .AddItem(nameof(SimpleClass.Itm1), new TypeConverterMapItem(BooleanTypeConverter.Instance))
                .AddItem(nameof(SimpleClass.Itm2), new TypeConverterMapItem(NumberAndEnumTypeConverter.Instance))
                .AddItem(nameof(SimpleClass.Itm3), new TypeConverterMapItem(StringTypeConverter.Instance));

            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            ABSaveObjectConverter.Serialize(new SimpleClass(), new TypeInformation(typeof(SimpleClass), TypeCode.Object), actual, map);

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteInt32(3);
            expected.WriteText("Itm1");
            expected.WriteByte(1);
            expected.WriteText("Itm2");
            expected.WriteInt32(12);
            expected.WriteText("Itm3");
            ABSaveItemSerializer.Serialize("abc", new TypeInformation(typeof(string), TypeCode.String), expected);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        public void Serialize_SimpleObject_Map()
        {
            var map = new ObjectMapItem(3)
                .AddItem<SimpleClass, bool>(nameof(SimpleClass.Itm1), o => o.Itm1, (o, v) => o.Itm1 = v, new TypeConverterMapItem(BooleanTypeConverter.Instance))
                .AddItem<SimpleClass, int>(nameof(SimpleClass.Itm2), o => o.Itm2, (o, v) => o.Itm2 = v, new TypeConverterMapItem(NumberAndEnumTypeConverter.Instance))
                .AddItem<SimpleClass, string>(nameof(SimpleClass.Itm3), o => o.Itm3, (o, v) => o.Itm3 = v, new TypeConverterMapItem(StringTypeConverter.Instance));

            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            ABSaveObjectConverter.Serialize(new SimpleClass(), new TypeInformation(typeof(SimpleClass), TypeCode.Object), actual, map);

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteInt32(3);
            expected.WriteText("Itm1");
            expected.WriteByte(1);
            expected.WriteText("Itm2");
            expected.WriteInt32(12);
            expected.WriteText("Itm3");
            ABSaveItemSerializer.Serialize("abc", new TypeInformation(typeof(string), TypeCode.String), expected);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }
    }
}
