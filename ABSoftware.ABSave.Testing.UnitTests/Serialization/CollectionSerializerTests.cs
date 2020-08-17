using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Exceptions;
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
    public class CollectionSerializerTests
    {
        [TestMethod]
        public void ListWrapper()
        {
            var genericWrapper = new GenericICollectionWrapper<string>();

            genericWrapper.SetCollection(new List<string>() { "abc" });
            Assert.IsTrue(typeof(string).IsEquivalentTo(genericWrapper.ElementType));
            Assert.AreEqual(1, genericWrapper.Count);

            var enumerator = genericWrapper.GetEnumerator();
            
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual("abc", enumerator.Current);
            Assert.IsFalse(enumerator.MoveNext());

            genericWrapper.CreateCollection(1, typeof(List<string>));
            genericWrapper.AddItem("abc");
            Assert.AreEqual(1, genericWrapper.Count);
        }

        [TestMethod]
        public void GetCollectionWrapper_List() => Assert.AreEqual(typeof(GenericICollectionWrapper<string>), CollectionTypeConverter.Instance.GetCollectionWrapper(typeof(List<string>)).GetType());

        [TestMethod]
        public void GetCollectionWrapper_NonGenericIList() => Assert.AreEqual(typeof(NonGenericIListWrapper), CollectionTypeConverter.Instance.GetCollectionWrapper(typeof(ArrayList)).GetType());

        [TestMethod]
        public void GetCollectionWrapper_None()
        {
            try
            {
                var result = CollectionTypeConverter.Instance.GetCollectionWrapper(typeof(CollectionSerializerTests));
            }
            catch (ABSaveUnrecognizedCollectionException) { return; }

            throw new Exception("Exception was not thrown!");
        }

        [TestMethod]
        public void SerializeArray_ValueType_UseConverter()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            var arrType = typeof(int[]);

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
            var arrType = typeof(SimpleStruct[]);

            var arr = new SimpleStruct[] { new SimpleStruct(1134), new SimpleStruct(5678) };

            Assert.IsTrue(CollectionTypeConverter.Instance.CheckCanConvertType(arrType));
            CollectionTypeConverter.Instance.Serialize(arr, arrType, actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteByte(0);
            expected.WriteInt32(2);
            ABSaveObjectConverter.Serialize(arr[0], typeof(SimpleStruct), expected);
            ABSaveObjectConverter.Serialize(arr[1], typeof(SimpleStruct), expected);

            WriterComparer.Compare(expected, actual);
        }

        [TestMethod]
        public void SerializeArray_NonValueType()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            var arrType = typeof(SimpleClass[]);

            var so = new SimpleClass[] { new SimpleClass(), new SimpleClass() };

            Assert.IsTrue(CollectionTypeConverter.Instance.CheckCanConvertType(arrType));
            CollectionTypeConverter.Instance.Serialize(so, arrType, actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteByte(0);
            expected.WriteInt32(2);
            ABSaveItemConverter.SerializeWithAttribute(so[0], typeof(SimpleClass), expected);
            ABSaveItemConverter.SerializeWithAttribute(so[1], typeof(SimpleClass), expected);

            WriterComparer.Compare(expected, actual);
        }

        [TestMethod]
        public void SerializeArray_CustomLowerBound()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            var arrType = typeof(int[]);

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
            var arrType = typeof(int[,]);

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
            var arrType = typeof(int[,]);

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
            var arrType = typeof(List<int>);

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
            var arrType = typeof(Dictionary<int, int>);

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
            var arrType = typeof(ArrayList);

            Assert.IsTrue(CollectionTypeConverter.Instance.CheckCanConvertType(arrType));
            CollectionTypeConverter.Instance.Serialize(new ArrayList() { 1, 2, 3, 4 }, arrType, actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteInt32(4);
            expected.WriteDifferentTypeAttribute();
            TypeTypeConverter.Instance.Serialize(typeof(int), typeof(Type), expected);
            expected.WriteInt32(1);

            expected.WriteDifferentTypeAttribute();
            TypeTypeConverter.Instance.Serialize(typeof(int), typeof(Type), expected);
            expected.WriteInt32(2);

            expected.WriteDifferentTypeAttribute();
            TypeTypeConverter.Instance.Serialize(typeof(int), typeof(Type), expected);
            expected.WriteInt32(3);

            expected.WriteDifferentTypeAttribute();
            TypeTypeConverter.Instance.Serialize(typeof(int), typeof(Type), expected);
            expected.WriteInt32(4);

            WriterComparer.Compare(expected, actual);
        }

        [TestMethod]
        public void SerializeArray_Map()
        {
            var map = CollectionMapItem.ForArray(typeof(int), new TypeConverterMapItem(false, NumberTypeConverter.Instance));

            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            map.Serialize(new int[] { 1, 2, 3, 4 }, typeof(int[]), actual);

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
}
