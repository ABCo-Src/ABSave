using ABSoftware.ABSave.Converters.Internal;
using ABSoftware.ABSave.Serialization;
using ABSoftware.ABSave.Serialization.Writer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Testing.UnitTests.Serialization
{
    [TestClass]
    public class ABSaveItemSerializerTests
    {
        [TestMethod]
        public void Attributes_Null()
        {
            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            ABSaveItemSerializer.SerializeAttributes(null, actual, new Helpers.TypeInformation());

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteNullAttribute();

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        public void Attributes_Nullable_NonNull()
        {
            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            ABSaveItemSerializer.SerializeAttributes(new bool?(true), actual, new Helpers.TypeInformation(typeof(bool?), TypeCode.Object));

            CollectionAssert.AreEqual(new byte[] { 2 }, actual.ToBytes());
        }

        [TestMethod]
        public void Attributes_ValueType()
        {
            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            ABSaveItemSerializer.SerializeAttributes(123, actual, new Helpers.TypeInformation(typeof(int), TypeCode.Int32));

            CollectionAssert.AreEqual(new byte[0], actual.ToBytes());
        }

        [TestMethod]
        public void Attributes_ReferenceType_Matching()
        {
            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            ABSaveItemSerializer.SerializeAttributes(new ReferenceTypeSub(), actual, new Helpers.TypeInformation(typeof(ReferenceTypeSub), TypeCode.Object));

            CollectionAssert.AreEqual(new byte[] { 2 }, actual.ToBytes());
        }

        [TestMethod]
        public void Attributes_ReferenceType_Different()
        {
            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            ABSaveItemSerializer.SerializeAttributes(new ReferenceTypeSub(), actual, new Helpers.TypeInformation(typeof(ReferenceTypeSub), TypeCode.Object, typeof(ReferenceTypeBase), TypeCode.Object));

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteDifferentTypeAttribute();
            TypeTypeConverter.Instance.SerializeClosedType(typeof(ReferenceTypeSub), expected);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        public void SerializeItem_ExactTypeConverter()
        {
            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            ABSaveItemSerializer.SerializeAuto("abcd", actual, new Helpers.TypeInformation(typeof(string), TypeCode.String));

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteMatchingTypeAttribute();
            expected.WriteText("abcd");

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        public void SerializeItem_NonExactTypeConverter()
        {
            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            ABSaveItemSerializer.SerializeAuto(1234f, actual, new Helpers.TypeInformation(typeof(float), TypeCode.Single));

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteSingle(1234f);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        public void SerializeItem_ReferenceTypeObject()
        {
            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            ABSaveItemSerializer.SerializeAuto(new ReferenceTypeSub(), actual, new Helpers.TypeInformation(typeof(ReferenceTypeSub), TypeCode.Object));

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteMatchingTypeAttribute();
            expected.WriteInt32(0);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        public void SerializeItem_ValueTypeObject()
        {
            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            ABSaveItemSerializer.SerializeAuto(new ValueTypeObj(), actual, new Helpers.TypeInformation(typeof(ValueTypeObj), TypeCode.Object));

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteInt32(0);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }
    }
}
