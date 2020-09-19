using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Mapping;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ABSoftware.ABSave.Testing.UnitTests.Serialization
{
    [TestClass]
    public class ObjectSerializerTests
    {
        [TestMethod]
        public void Serialize_SimpleStruct()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            ABSaveObjectConverter.Serialize(new SimpleStruct(12), typeof(SimpleStruct), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteInt32(1);
            expected.WriteString("Inside");
            expected.WriteInt32(12);

            TestUtilities.CompareWriters(expected, actual);
        }

        [TestMethod]
        public void Serialize_SimpleObject()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());

            var simpleClass = new SimpleClass()
            {
                Itm1 = true,
                Itm2 = 12,
                Itm3 = "abc"
            };

            ABSaveObjectConverter.Serialize(simpleClass, typeof(SimpleClass), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteInt32(3);
            expected.WriteString("Itm1");
            expected.WriteByte(1);
            expected.WriteString("Itm2");
            expected.WriteInt32(12);
            expected.WriteString("Itm3");
            ABSaveItemConverter.SerializeWithAttribute("abc", typeof(string), expected);

            TestUtilities.CompareWriters(expected, actual);
        }

        [TestMethod]
        public void Serialize_SimpleObject_MapReflection()
        {
            var map = new ObjectMapItem(false, () => new SimpleClass(), 3)
                .AddItem(nameof(SimpleClass.Itm1), new TypeConverterMapItem(false, BooleanTypeConverter.Instance))
                .AddItem(nameof(SimpleClass.Itm2), new TypeConverterMapItem(false, NumberTypeConverter.Instance))
                .AddItem(nameof(SimpleClass.Itm3), new TypeConverterMapItem(true, StringTypeConverter.Instance));

            var simpleClass = new SimpleClass()
            {
                Itm1 = false,
                Itm2 = 3863,
                Itm3 = "def"
            };

            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            map.Serialize(simpleClass, typeof(SimpleClass), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteInt32(3);
            expected.WriteString("Itm1");
            expected.WriteByte(0);
            expected.WriteString("Itm2");
            expected.WriteInt32(3863);
            expected.WriteString("Itm3");
            ABSaveItemConverter.SerializeWithAttribute("def", typeof(string), expected);

            TestUtilities.CompareWriters(expected, actual);
        }

        [TestMethod]
        public void Serialize_SimpleObject_Map()
        {
            var map = new ObjectMapItem(false, () => new SimpleClass(), 3)
                .AddItem<SimpleClass, bool>(nameof(SimpleClass.Itm1), o => o.Itm1, (o, v) => o.Itm1 = v, new TypeConverterMapItem(false, BooleanTypeConverter.Instance))
                .AddItem<SimpleClass, int>(nameof(SimpleClass.Itm2), o => o.Itm2, (o, v) => o.Itm2 = v, new TypeConverterMapItem(false, NumberTypeConverter.Instance))
                .AddItem<SimpleClass, string>(nameof(SimpleClass.Itm3), o => o.Itm3, (o, v) => o.Itm3 = v, new TypeConverterMapItem(true, StringTypeConverter.Instance));

            var simpleClass = new SimpleClass()
            {
                Itm1 = false,
                Itm2 = 1234,
                Itm3 = "ghi"
            };

            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            map.Serialize(simpleClass, typeof(SimpleClass), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteInt32(3);
            expected.WriteString("Itm1");
            expected.WriteByte(0);
            expected.WriteString("Itm2");
            expected.WriteInt32(1234);
            expected.WriteString("Itm3");
            ABSaveItemConverter.SerializeWithAttribute("ghi", typeof(string), expected);

            TestUtilities.CompareWriters(expected, actual);
        }
    }
}
