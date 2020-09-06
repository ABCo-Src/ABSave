using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Mapping;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ABSoftware.ABSave.Testing.UnitTests.Deserialization
{
    [TestClass]
    public class ObjectDeserializerTests
    {
        [TestMethod]
        public void Deserialize_SimpleStruct()
        {
            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());

            ABSaveObjectConverter.Serialize(new SimpleStruct(12), typeof(SimpleStruct), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            var obj = (SimpleStruct)ABSaveObjectConverter.Deserialize(typeof(SimpleStruct), reader);

            Assert.AreEqual(obj.Inside, 12);
        }

        [TestMethod]
        public void Deserialize_SimpleObject()
        {
            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());

            ABSaveObjectConverter.Serialize(new SimpleClass()
            {
                Itm1 = true,
                Itm2 = 21596,
                Itm3 = "hmm"
            }, typeof(SimpleClass), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            var obj = (SimpleClass)ABSaveObjectConverter.Deserialize(typeof(SimpleClass), reader);

            Assert.AreEqual(obj.Itm1, true);
            Assert.AreEqual(obj.Itm2, 21596);
            Assert.AreEqual(obj.Itm3, "hmm");
        }

        [TestMethod]
        public void Deserialize_SimpleObject_MapReflection()
        {
            var map = new ObjectMapItem(false, () => new SimpleClass(), 3)
                .AddItem(nameof(SimpleClass.Itm1), new TypeConverterMapItem(false, BooleanTypeConverter.Instance))
                .AddItem(nameof(SimpleClass.Itm2), new TypeConverterMapItem(false, NumberTypeConverter.Instance))
                .AddItem(nameof(SimpleClass.Itm3), new TypeConverterMapItem(true, StringTypeConverter.Instance));

            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());

            map.Serialize(new SimpleClass()
            {
                Itm1 = false,
                Itm2 = 3863,
                Itm3 = "def"
            }, typeof(SimpleClass), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            var obj = (SimpleClass)map.Deserialize(typeof(SimpleClass), reader);

            Assert.AreEqual(obj.Itm1, false);
            Assert.AreEqual(obj.Itm2, 3863);
            Assert.AreEqual(obj.Itm3, "def");
        }

        [TestMethod]
        public void Deserialize_SimpleObject_Map()
        {
            var map = new ObjectMapItem(false, () => new SimpleClass(), 3)
                .AddItem<SimpleClass, bool>(nameof(SimpleClass.Itm1), o => o.Itm1, (o, v) => o.Itm1 = v, new TypeConverterMapItem(false, BooleanTypeConverter.Instance))
                .AddItem<SimpleClass, int>(nameof(SimpleClass.Itm2), o => o.Itm2, (o, v) => o.Itm2 = v, new TypeConverterMapItem(false, NumberTypeConverter.Instance))
                .AddItem<SimpleClass, string>(nameof(SimpleClass.Itm3), o => o.Itm3, (o, v) => o.Itm3 = v, new TypeConverterMapItem(true, StringTypeConverter.Instance));

            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());

            map.Serialize(new SimpleClass()
            {
                Itm1 = false,
                Itm2 = 3863,
                Itm3 = "def"
            }, typeof(SimpleClass), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            var obj = (SimpleClass)map.Deserialize(typeof(SimpleClass), reader);

            Assert.AreEqual(obj.Itm1, false);
            Assert.AreEqual(obj.Itm2, 3863);
            Assert.AreEqual(obj.Itm3, "def");
        }
    }
}
