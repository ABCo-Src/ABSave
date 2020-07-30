using ABSoftware.ABSave.Converters.Internal;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization;
using ABSoftware.ABSave.Serialization.Writer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Testing.UnitTests.Serialization
{
    [TestClass]
    public class ABSaveCollectionSerializerTests
    {
        [TestMethod]
        public void SerializeArray_ValueType_UseConverter()
        {
            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            var arrType = new TypeInformation(typeof(int[]), TypeCode.Object);

            Assert.IsTrue(CollectionTypeConverter.Instance.CheckCanConvertType(arrType));
            CollectionTypeConverter.Instance.Serialize(new int[] { 1, 2, 3, 4 }, arrType, actual);

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteByte(0);
            expected.WriteInt32(4);
            expected.WriteInt32(1);
            expected.WriteInt32(2);
            expected.WriteInt32(3);
            expected.WriteInt32(4);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        public void SerializeArray_ValueType_NoConverter()
        {
            var actual = new ABSaveMemoryWriter(new ABSaveSettings().SetWithNames(false));
            var arrType = new TypeInformation(typeof(SimpleStruct[]), TypeCode.Object);

            Assert.IsTrue(CollectionTypeConverter.Instance.CheckCanConvertType(arrType));
            CollectionTypeConverter.Instance.Serialize(new SimpleStruct[] { new SimpleStruct(1134), new SimpleStruct(5678) }, arrType, actual);

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteByte(0);
            expected.WriteInt32(2);
            expected.WriteInt32(1);
            expected.WriteInt32(1134);
            expected.WriteInt32(1);
            expected.WriteInt32(5678);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        public void SerializeArray_NonValueType()
        {
            var actual = new ABSaveMemoryWriter(new ABSaveSettings().SetWithNames(false));
            var arrType = new TypeInformation(typeof(SimpleClass[]), TypeCode.Object);

            var so = new SimpleClass[] { new SimpleClass(), new SimpleClass() };

            Assert.IsTrue(CollectionTypeConverter.Instance.CheckCanConvertType(arrType));
            CollectionTypeConverter.Instance.Serialize(so, arrType, actual);

            var expected = new ABSaveMemoryWriter(new ABSaveSettings().SetWithNames(false));
            expected.WriteByte(0);
            expected.WriteInt32(2);
            ABSaveItemSerializer.SerializeAuto(so[0], new TypeInformation(typeof(SimpleClass), TypeCode.Object), expected);
            ABSaveItemSerializer.SerializeAuto(so[1], new TypeInformation(typeof(SimpleClass), TypeCode.Object), expected);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        public void SerializeArray_CustomLowerBound()
        {
            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            var arrType = new TypeInformation(typeof(int[]), TypeCode.Object);

            var arr = Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 2 });

            arr.SetValue(1, 2);
            arr.SetValue(2, 3);
            arr.SetValue(3, 4);
            arr.SetValue(4, 5);

            Assert.IsTrue(CollectionTypeConverter.Instance.CheckCanConvertType(arrType));
            CollectionTypeConverter.Instance.Serialize(arr, arrType, actual);

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteByte(1);
            expected.WriteInt32(2);
            expected.WriteInt32(4);
            expected.WriteInt32(1);
            expected.WriteInt32(2);
            expected.WriteInt32(3);
            expected.WriteInt32(4);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        public void SerializeArray_MultiDimensional()
        {
            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            var arrType = new TypeInformation(typeof(int[,]), TypeCode.Object);

            Assert.IsTrue(CollectionTypeConverter.Instance.CheckCanConvertType(arrType));
            CollectionTypeConverter.Instance.Serialize(new int[2, 2] { { 1, 2 }, { 3, 4 } }, arrType, actual);

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteByte(2);
            expected.WriteInt32(2);
            expected.WriteInt32(2);
            expected.WriteInt32(2);
            expected.WriteInt32(1);
            expected.WriteInt32(2);
            expected.WriteInt32(3);
            expected.WriteInt32(4);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        public void SerializeArray_MultiDimensional_CustomLowerBound()
        {
            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            var arrType = new TypeInformation(typeof(int[,]), TypeCode.Object);

            var arr = Array.CreateInstance(typeof(int), new int[] { 2, 2 }, new int[] { 7, 8 });
            arr.SetValue(1, 7, 8);
            arr.SetValue(2, 7, 9);
            arr.SetValue(3, 8, 8);
            arr.SetValue(4, 8, 9);

            Assert.IsTrue(CollectionTypeConverter.Instance.CheckCanConvertType(arrType));
            CollectionTypeConverter.Instance.Serialize(arr, arrType, actual);

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteByte(3);
            expected.WriteInt32(2);
            expected.WriteInt32(7);
            expected.WriteInt32(8);
            expected.WriteInt32(2);
            expected.WriteInt32(2);
            expected.WriteInt32(1);
            expected.WriteInt32(2);
            expected.WriteInt32(3);
            expected.WriteInt32(4);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        public void SerializeIList_Generic()
        {
            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            var arrType = new TypeInformation(typeof(List<int>), TypeCode.Object);

            Assert.IsTrue(CollectionTypeConverter.Instance.CheckCanConvertType(arrType));
            CollectionTypeConverter.Instance.Serialize(new List<int>() { 1, 2, 3, 4 }, arrType, actual);

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteInt32(4);
            expected.WriteInt32(1);
            expected.WriteInt32(2);
            expected.WriteInt32(3);
            expected.WriteInt32(4);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        public void SerializeICollection_NonIList_Generic()
        {
            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            var arrType = new TypeInformation(typeof(Dictionary<int, int>), TypeCode.Object);

            Assert.IsTrue(CollectionTypeConverter.Instance.CheckCanConvertType(arrType));
            CollectionTypeConverter.Instance.Serialize(new Dictionary<int, int>() { { 1, 2 }, { 3, 4 } }, arrType, actual);

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteInt32(2);
            expected.WriteInt32(1);
            expected.WriteInt32(2);
            expected.WriteInt32(3);
            expected.WriteInt32(4);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        public void SerializeIList_NonGeneric()
        {
            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            var arrType = new TypeInformation(typeof(ArrayList), TypeCode.Object);

            Assert.IsTrue(CollectionTypeConverter.Instance.CheckCanConvertType(arrType));
            CollectionTypeConverter.Instance.Serialize(new ArrayList() { 1, 2, 3, 4 }, arrType, actual);

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteInt32(4);
            expected.WriteDifferentTypeAttribute();
            TypeTypeConverter.Instance.Serialize(typeof(int), new TypeInformation(), expected);
            expected.WriteInt32(1);

            expected.WriteDifferentTypeAttribute();
            TypeTypeConverter.Instance.Serialize(typeof(int), new TypeInformation(), expected);
            expected.WriteInt32(2);

            expected.WriteDifferentTypeAttribute();
            TypeTypeConverter.Instance.Serialize(typeof(int), new TypeInformation(), expected);
            expected.WriteInt32(3);

            expected.WriteDifferentTypeAttribute();
            TypeTypeConverter.Instance.Serialize(typeof(int), new TypeInformation(), expected);
            expected.WriteInt32(4);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }
    }

    class NonListEnumerable
    {

    }
}
