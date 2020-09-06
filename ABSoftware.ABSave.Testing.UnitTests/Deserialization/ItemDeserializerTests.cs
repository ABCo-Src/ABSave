using ABSoftware.ABSave.Converters;
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
        public void DeserializeAttribute_Null()
        {
            var reader = new ABSaveReader(new MemoryStream(new byte[] { 1 } ), new ABSaveSettings());
            Assert.IsNull(ABSaveItemConverter.DeserializeAttribute(reader, typeof(object)));
        }

        [TestMethod]
        public void DeserializeAttribute_MatchingItem()
        {
            var reader = new ABSaveReader(new MemoryStream(new byte[] { 2 }), new ABSaveSettings());
            Assert.AreEqual(typeof(object), ABSaveItemConverter.DeserializeAttribute(reader, typeof(object)));
        }

        [TestMethod]
        public void DeserializeAttribute_DifferentType()
        {
            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());

            writer.WriteByte(3);
            TypeTypeConverter.Instance.SerializeClosedType(typeof(string), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());

            Assert.AreEqual(typeof(string), ABSaveItemConverter.DeserializeAttribute(reader, typeof(object)));
        }

        [TestMethod]
        public void DeserializeAttribute_ValueType()
        {
            var memoryStream = new MemoryStream();
            var actual = new ABSaveWriter(memoryStream, new ABSaveSettings());
            ABSaveItemConverter.SerializeAttribute(123, typeof(int), typeof(int), actual);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            Assert.AreEqual(typeof(int), ABSaveItemConverter.DeserializeAttribute(reader, typeof(int)));
        }

        [TestMethod]
        public void DeserializeAttribute_Nullable_NonNull()
        {
            var memoryStream = new MemoryStream();
            var actual = new ABSaveWriter(memoryStream, new ABSaveSettings());
            ABSaveItemConverter.SerializeAttribute(new int?(123), typeof(int), typeof(int?), actual);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            Assert.AreEqual(typeof(int), ABSaveItemConverter.DeserializeAttribute(reader, typeof(int?)));
        }

        [TestMethod]
        public void DeserializeItem_ExactTypeConverter()
        {
            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());
            writer.WriteMatchingTypeAttribute();
            writer.WriteText("abcd");

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            var str = ABSaveItemConverter.DeserializeWithAttribute(typeof(string), reader);

            Assert.AreEqual("abcd", str);
        }

        [TestMethod]
        public void DeserializeItem_NonExactTypeConverter()
        {
            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());
            writer.WriteSingle(1234f);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            var res = ABSaveItemConverter.DeserializeWithAttribute(typeof(float), reader);

            Assert.AreEqual(1234f, res);
        }

        [TestMethod]
        public void DeserializeItem_ReferenceTypeObject()
        {
            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());
            writer.WriteMatchingTypeAttribute();
            writer.WriteByte(0);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            var res = ABSaveItemConverter.DeserializeWithAttribute(typeof(ReferenceTypeSub), reader);

            Assert.AreEqual(typeof(ReferenceTypeSub), res.GetType());
        }

        [TestMethod]
        public void DeserializeItem_ValueTypeObject()
        {
            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());
            writer.WriteByte(0);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            var res = ABSaveItemConverter.DeserializeWithAttribute(typeof(ValueTypeObj), reader);

            Assert.AreEqual(typeof(ValueTypeObj), res.GetType());
        }
    }
}
