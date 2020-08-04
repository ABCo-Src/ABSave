using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ABSoftware.ABSave.Testing.UnitTests.Serialization
{
    [TestClass]
    public class ABSaveItemSerializerTests
    {
        [TestMethod]
        public void Attributes_Null()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            ABSaveItemSerializer.SerializeAttributes(null, new TypeInformation(), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteNullAttribute();

            WriterComparer.Compare(expected, actual);
        }

        [TestMethod]
        public void Attributes_Nullable_NonNull()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            ABSaveItemSerializer.SerializeAttributes(new bool?(true), new TypeInformation(typeof(bool?), TypeCode.Object), actual);

            CollectionAssert.AreEqual(new byte[] { 2 }, ((MemoryStream)actual.Output).ToArray());
        }

        [TestMethod]
        public void Attributes_ValueType()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            ABSaveItemSerializer.SerializeAttributes(123, new TypeInformation(typeof(int), TypeCode.Int32), actual);

            CollectionAssert.AreEqual(new byte[0], ((MemoryStream)actual.Output).ToArray());
        }

        [TestMethod]
        public void Attributes_ReferenceType_Matching()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            ABSaveItemSerializer.SerializeAttributes(new ReferenceTypeSub(), new TypeInformation(typeof(ReferenceTypeSub), TypeCode.Object), actual);

            CollectionAssert.AreEqual(new byte[] { 2 }, ((MemoryStream)actual.Output).ToArray());
        }

        [TestMethod]
        public void Attributes_ReferenceType_Different()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            ABSaveItemSerializer.SerializeAttributes(new ReferenceTypeSub(), new TypeInformation(typeof(ReferenceTypeSub), TypeCode.Object, typeof(ReferenceTypeBase), TypeCode.Object), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteDifferentTypeAttribute();
            TypeTypeConverter.Instance.SerializeClosedType(typeof(ReferenceTypeSub), expected);

            WriterComparer.Compare(expected, actual);
        }

        [TestMethod]
        public void SerializeItem_ExactTypeConverter()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            ABSaveItemSerializer.Serialize("abcd", new TypeInformation(typeof(string), TypeCode.String), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteMatchingTypeAttribute();
            expected.WriteText("abcd");

            WriterComparer.Compare(expected, actual);
        }

        [TestMethod]
        public void SerializeItem_NonExactTypeConverter()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            ABSaveItemSerializer.Serialize(1234f, new TypeInformation(typeof(float), TypeCode.Single), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteSingle(1234f);

            WriterComparer.Compare(expected, actual);
        }

        [TestMethod]
        public void SerializeItem_ReferenceTypeObject()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            ABSaveItemSerializer.Serialize(new ReferenceTypeSub(), new TypeInformation(typeof(ReferenceTypeSub), TypeCode.Object), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteMatchingTypeAttribute();
            expected.WriteInt32(0);

            WriterComparer.Compare(expected, actual);
        }

        [TestMethod]
        public void SerializeItem_ValueTypeObject()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            ABSaveItemSerializer.Serialize(new ValueTypeObj(), new TypeInformation(typeof(ValueTypeObj), TypeCode.Object), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteInt32(0);

            WriterComparer.Compare(expected, actual);
        }
    }
}
