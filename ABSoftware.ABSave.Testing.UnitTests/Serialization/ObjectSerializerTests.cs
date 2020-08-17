using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Serialization;
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
            expected.WriteText("Inside");
            expected.WriteInt32(12);

            WriterComparer.Compare(expected, actual);
        }

        [TestMethod]
        public void Serialize_SimpleObject()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            ABSaveObjectConverter.Serialize(new SimpleClass(), typeof(SimpleClass), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteInt32(3);
            expected.WriteText("Itm1");
            expected.WriteByte(1);
            expected.WriteText("Itm2");
            expected.WriteInt32(12);
            expected.WriteText("Itm3");
            ABSaveItemConverter.SerializeWithAttribute("abc", typeof(string), expected);

            WriterComparer.Compare(expected, actual);
        }

        [TestMethod]
        public void Serialize_SimpleObject_MapReflection()
        {
            var map = new ObjectMapItem(false, 3)
                .AddItem(nameof(SimpleClass.Itm1), new TypeConverterMapItem(false, BooleanTypeConverter.Instance))
                .AddItem(nameof(SimpleClass.Itm2), new TypeConverterMapItem(false, NumberTypeConverter.Instance))
                .AddItem(nameof(SimpleClass.Itm3), new TypeConverterMapItem(true, StringTypeConverter.Instance));

            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            map.Serialize(new SimpleClass(), typeof(SimpleClass), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteInt32(3);
            expected.WriteText("Itm1");
            expected.WriteByte(1);
            expected.WriteText("Itm2");
            expected.WriteInt32(12);
            expected.WriteText("Itm3");
            ABSaveItemConverter.SerializeWithAttribute("abc", typeof(string), expected);

            WriterComparer.Compare(expected, actual);
        }

        [TestMethod]
        public void Serialize_SimpleObject_Map()
        {
            var map = new ObjectMapItem(false, 3)
                .AddItem<SimpleClass, bool>(nameof(SimpleClass.Itm1), o => o.Itm1, (o, v) => o.Itm1 = v, new TypeConverterMapItem(BooleanTypeConverter.Instance))
                .AddItem<SimpleClass, int>(nameof(SimpleClass.Itm2), o => o.Itm2, (o, v) => o.Itm2 = v, new TypeConverterMapItem(NumberTypeConverter.Instance))
                .AddItem<SimpleClass, string>(nameof(SimpleClass.Itm3), o => o.Itm3, (o, v) => o.Itm3 = v, new TypeConverterMapItem(StringTypeConverter.Instance));

            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            map.Serialize(new SimpleClass(), typeof(SimpleClass), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteInt32(3);
            expected.WriteText("Itm1");
            expected.WriteByte(1);
            expected.WriteText("Itm2");
            expected.WriteInt32(12);
            expected.WriteText("Itm3");
            ABSaveItemConverter.SerializeWithAttribute("abc", typeof(string), expected);

            WriterComparer.Compare(expected, actual);
        }
    }
}
