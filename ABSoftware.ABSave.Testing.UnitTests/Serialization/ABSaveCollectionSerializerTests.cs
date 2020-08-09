using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ABSoftware.ABSave.Testing.UnitTests.Serialization
{
    [TestClass]
    public class ABSaveCollectionSerializerTests
    {
        [TestMethod]
        public void GetCollectionType_GenericIList()
        {
            var result = CollectionTypeConverter.Instance.GetCollectionType(typeof(List<string>), out Type genericItemType);

            Assert.AreEqual(CollectionType.GenericIList, result);
            Assert.IsTrue(typeof(string).IsEquivalentTo(genericItemType));
        }

        [TestMethod]
        public void GetCollectionType_GenericICollection()
        {
            var result = CollectionTypeConverter.Instance.GetCollectionType(typeof(Dictionary<string, string>), out Type genericItemType);

            Assert.AreEqual(CollectionType.Generic, result);
            Assert.IsTrue(typeof(KeyValuePair<string, string>).IsEquivalentTo(genericItemType));
        }

        [TestMethod]
        public void GetCollectionType_NonGenericIList()
        {
            var result = CollectionTypeConverter.Instance.GetCollectionType(typeof(ArrayList), out _);
            Assert.AreEqual(CollectionType.NonGenericIList, result);
        }

        [TestMethod]
        public void GetCollectionType_NonGenericICollection()
        {
            var result = CollectionTypeConverter.Instance.GetCollectionType(typeof(Hashtable), out _);
            Assert.AreEqual(CollectionType.NonGeneric, result);
        }

        [TestMethod]
        public void GetCollectionType_None()
        {
            var result = CollectionTypeConverter.Instance.GetCollectionType(typeof(ABSaveCollectionSerializerTests), out _);
            Assert.AreEqual(CollectionType.None, result);
        }

        [TestMethod]
        public void SerializeArray_ValueType_UseConverter()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            var arrType = new TypeInformation(typeof(int[]), TypeCode.Object);

            Assert.IsTrue(CollectionTypeConverter.Instance.CheckCanConvertType(arrType));
            CollectionTypeConverter.Instance.Serialize(new int[] { 1, 2, 3, 4 }, arrType, actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteByte(0);
            expected.WriteInt32(4);
            expected.WriteInt32(1);
            expected.WriteInt32(2);
            expected.WriteInt32(3);
            expected.WriteInt32(4);

            WriterComparer.Compare(expected, actual);
        }

        [TestMethod]
        public void SerializeArray_ValueType_NoConverter()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            var arrType = new TypeInformation(typeof(SimpleStruct[]), TypeCode.Object);

            var arr = new SimpleStruct[] { new SimpleStruct(1134), new SimpleStruct(5678) };

            Assert.IsTrue(CollectionTypeConverter.Instance.CheckCanConvertType(arrType));
            CollectionTypeConverter.Instance.Serialize(arr, arrType, actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteByte(0);
            expected.WriteInt32(2);
            ABSaveObjectConverter.Serialize(arr[0], new TypeInformation(typeof(SimpleStruct), TypeCode.Object), expected);
            ABSaveObjectConverter.Serialize(arr[1], new TypeInformation(typeof(SimpleStruct), TypeCode.Object), expected);

            WriterComparer.Compare(expected, actual);
        }

        [TestMethod]
        public void SerializeArray_NonValueType()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            var arrType = new TypeInformation(typeof(SimpleClass[]), TypeCode.Object);

            var so = new SimpleClass[] { new SimpleClass(), new SimpleClass() };

            Assert.IsTrue(CollectionTypeConverter.Instance.CheckCanConvertType(arrType));
            CollectionTypeConverter.Instance.Serialize(so, arrType, actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteByte(0);
            expected.WriteInt32(2);
            ABSaveItemConverter.Serialize(so[0], new TypeInformation(typeof(SimpleClass), TypeCode.Object), expected);
            ABSaveItemConverter.Serialize(so[1], new TypeInformation(typeof(SimpleClass), TypeCode.Object), expected);

            WriterComparer.Compare(expected, actual);
        }

        [TestMethod]
        public void SerializeArray_CustomLowerBound()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            var arrType = new TypeInformation(typeof(int[]), TypeCode.Object);

            var arr = Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 2 });

            arr.SetValue(1, 2);
            arr.SetValue(2, 3);
            arr.SetValue(3, 4);
            arr.SetValue(4, 5);

            Assert.IsTrue(CollectionTypeConverter.Instance.CheckCanConvertType(arrType));
            CollectionTypeConverter.Instance.Serialize(arr, arrType, actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteByte(1);
            expected.WriteInt32(2);
            expected.WriteInt32(4);
            expected.WriteInt32(1);
            expected.WriteInt32(2);
            expected.WriteInt32(3);
            expected.WriteInt32(4);

            WriterComparer.Compare(expected, actual);
        }

        [TestMethod]
        public void SerializeArray_MultiDimensional()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            var arrType = new TypeInformation(typeof(int[,]), TypeCode.Object);

            Assert.IsTrue(CollectionTypeConverter.Instance.CheckCanConvertType(arrType));
            CollectionTypeConverter.Instance.Serialize(new int[2, 2] { { 1, 2 }, { 3, 4 } }, arrType, actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteByte(2);
            expected.WriteInt32(2);
            expected.WriteInt32(2);
            expected.WriteInt32(2);
            expected.WriteInt32(1);
            expected.WriteInt32(2);
            expected.WriteInt32(3);
            expected.WriteInt32(4);

            WriterComparer.Compare(expected, actual);
        }

        [TestMethod]
        public void SerializeArray_MultiDimensional_CustomLowerBound()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            var arrType = new TypeInformation(typeof(int[,]), TypeCode.Object);

            var arr = Array.CreateInstance(typeof(int), new int[] { 2, 2 }, new int[] { 7, 8 });
            arr.SetValue(1, 7, 8);
            arr.SetValue(2, 7, 9);
            arr.SetValue(3, 8, 8);
            arr.SetValue(4, 8, 9);

            Assert.IsTrue(CollectionTypeConverter.Instance.CheckCanConvertType(arrType));
            CollectionTypeConverter.Instance.Serialize(arr, arrType, actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
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

            WriterComparer.Compare(expected, actual);
        }

        [TestMethod]
        public void SerializeIList_Generic()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            var arrType = new TypeInformation(typeof(List<int>), TypeCode.Object);

            Assert.IsTrue(CollectionTypeConverter.Instance.CheckCanConvertType(arrType));
            CollectionTypeConverter.Instance.Serialize(new List<int>() { 1, 2, 3, 4 }, arrType, actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteInt32(4);
            expected.WriteInt32(1);
            expected.WriteInt32(2);
            expected.WriteInt32(3);
            expected.WriteInt32(4);

            WriterComparer.Compare(expected, actual);
        }

        [TestMethod]
        public void SerializeICollection_NonIList_Generic()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            var arrType = new TypeInformation(typeof(Dictionary<int, int>), TypeCode.Object);

            Assert.IsTrue(CollectionTypeConverter.Instance.CheckCanConvertType(arrType));
            CollectionTypeConverter.Instance.Serialize(new Dictionary<int, int>() { { 1, 2 }, { 3, 4 } }, arrType, actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteInt32(2);
            expected.WriteInt32(1);
            expected.WriteInt32(2);
            expected.WriteInt32(3);
            expected.WriteInt32(4);

            WriterComparer.Compare(expected, actual);
        }

        [TestMethod]
        public void SerializeIList_NonGeneric()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            var arrType = new TypeInformation(typeof(ArrayList), TypeCode.Object);

            Assert.IsTrue(CollectionTypeConverter.Instance.CheckCanConvertType(arrType));
            CollectionTypeConverter.Instance.Serialize(new ArrayList() { 1, 2, 3, 4 }, arrType, actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
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

            WriterComparer.Compare(expected, actual);
        }

        [TestMethod]
        public void SerializeArray_Map()
        {
            var map = new CollectionMapItem(CollectionType.Array, typeof(int), new TypeConverterMapItem(NumberAndEnumTypeConverter.Instance));

            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            CollectionTypeConverter.Instance.Serialize(new int[] { 1, 2, 3, 4 }, actual, map);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteByte(0);
            expected.WriteInt32(4);
            expected.WriteInt32(1);
            expected.WriteInt32(2);
            expected.WriteInt32(3);
            expected.WriteInt32(4);

            WriterComparer.Compare(expected, actual);
        }
    }

    class NonListEnumerable
    {

    }
}
