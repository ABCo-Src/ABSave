using ABSoftware.ABSave.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace ABSoftware.ABSave.Testing.UnitTests.Serialization
{
    [TestClass]
    public class ItemSerializerTests
    {
        [TestMethod]
        public void SerializeAttribute_Null()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            ABSaveItemConverter.SerializeAttribute(null, typeof(object), typeof(object), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteNullAttribute();

            TestUtilities.CompareWriters(expected, actual);
        }

        [TestMethod]
        public void SerializeAttribute_Nullable_NonNull()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            ABSaveItemConverter.SerializeAttribute(new bool?(true), typeof(bool), typeof(bool?), actual);

            CollectionAssert.AreEqual(new byte[] { 2 }, ((MemoryStream)actual.Output).ToArray());
        }

        [TestMethod]
        public void SerializeAttribute_ValueType()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            ABSaveItemConverter.SerializeAttribute(123, typeof(int), typeof(int), actual);

            CollectionAssert.AreEqual(new byte[0], ((MemoryStream)actual.Output).ToArray());
        }

        [TestMethod]
        public void SerializeAttribute_MatchingType()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            ABSaveItemConverter.SerializeAttribute(new ReferenceTypeSub(), typeof(ReferenceTypeSub), typeof(ReferenceTypeSub), actual);

            CollectionAssert.AreEqual(new byte[] { 2 }, ((MemoryStream)actual.Output).ToArray());
        }

        [TestMethod]
        public void SerializeAttribute_DifferentType()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            ABSaveItemConverter.SerializeAttribute(new ReferenceTypeSub(), typeof(ReferenceTypeSub), typeof(ReferenceTypeBase), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteDifferentTypeAttribute();
            TypeTypeConverter.Instance.SerializeClosedType(typeof(ReferenceTypeSub), expected);

            TestUtilities.CompareWriters(expected, actual);
        }

        [TestMethod]
        public void SerializeItem_ExactTypeConverter()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            ABSaveItemConverter.Serialize("abcd", typeof(string), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteMatchingTypeAttribute();
            expected.WriteString("abcd");

            TestUtilities.CompareWriters(expected, actual);
        }

        [TestMethod]
        public void SerializeItem_NonExactTypeConverter()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            ABSaveItemConverter.Serialize(1234f, typeof(float), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteSingle(1234f);

            TestUtilities.CompareWriters(expected, actual);
        }

        [TestMethod]
        public void SerializeItem_ReferenceTypeObject()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            ABSaveItemConverter.Serialize(new ReferenceTypeSub(), typeof(ReferenceTypeSub), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteMatchingTypeAttribute();
            expected.WriteInt32(0);

            TestUtilities.CompareWriters(expected, actual);
        }

        [TestMethod]
        public void SerializeItem_ValueTypeObject()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            ABSaveItemConverter.Serialize(new ValueTypeObj(), typeof(ValueTypeObj), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteInt32(0);

            TestUtilities.CompareWriters(expected, actual);
        }
    }
}
