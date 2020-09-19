using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Mapping;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ABSoftware.ABSave.Testing.UnitTests.Deserialization
{
    [TestClass]
    public class CollectionDeserializerTests
    {
        [TestMethod]
        public void DeserializeArray_ValueType_UseConverter()
        {
            var expected = new int[] { 1, 2, 3, 4 };

            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());
            ArrayTypeConverter.Instance.Serialize(expected, typeof(int[]), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            var actual = (int[])ArrayTypeConverter.Instance.Deserialize(typeof(int[]), reader);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DeserializeArray_ValueType_NoConverter()
        {
            var expected = new SimpleStruct[] { new SimpleStruct(123), new SimpleStruct(456) };

            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());
            ArrayTypeConverter.Instance.Serialize(expected, typeof(SimpleStruct[]), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            var actual = (SimpleStruct[])ArrayTypeConverter.Instance.Deserialize(typeof(SimpleStruct[]), reader);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DeserializeArray_Map()
        {
            var map = new ArrayMapItem(false, typeof(int), true, new TypeConverterMapItem(false, NumberTypeConverter.Instance));

            var expected = new int[] { 1, 2, 3, 4 };

            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());
            ArrayTypeConverter.Instance.Serialize(expected, writer, map);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            var actual = (int[])ArrayTypeConverter.Instance.Deserialize(reader, map);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DeserializeArray_NonValueType()
        {
            var expected = new SimpleClass[] { new SimpleClass(false, 123, "abc"), new SimpleClass(true, 456, "def") };

            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());
            ArrayTypeConverter.Instance.Serialize(expected, typeof(SimpleClass[]), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            var actual = (SimpleClass[])ArrayTypeConverter.Instance.Deserialize(typeof(SimpleClass[]), reader);

            Assert.IsTrue(expected[0].IsEquivalentTo(actual[0]));
            Assert.IsTrue(expected[1].IsEquivalentTo(actual[1]));
        }

        [TestMethod]
        public void DeserializeArray_CustomLowerBound()
        {
            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());

            var expected = Array.CreateInstance(typeof(int), new int[] { 4 }, new int[] { 2 });

            expected.SetValue(1, 2);
            expected.SetValue(2, 3);
            expected.SetValue(3, 4);
            expected.SetValue(4, 5);

            ArrayTypeConverter.Instance.Serialize(expected, typeof(int[]), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            var actual = (Array)ArrayTypeConverter.Instance.Deserialize(typeof(int[]), reader);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DeserializeArray_MultiDimensional()
        {
            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());

            var expected = new int[2, 2] { { 1, 2 }, { 3, 4 } };

            ArrayTypeConverter.Instance.Serialize(expected, typeof(int[,]), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            var actual = (Array)ArrayTypeConverter.Instance.Deserialize(typeof(int[,]), reader);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DeserializeArray_MultiDimensional_CustomLowerBound()
        {
            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());

            var expected = Array.CreateInstance(typeof(int), new int[] { 2, 2 }, new int[] { 2, 2 });

            expected.SetValue(1, new int[] { 2, 2 });
            expected.SetValue(2, new int[] { 2, 3 });
            expected.SetValue(3, new int[] { 3, 2 });
            expected.SetValue(4, new int[] { 3, 3 });

            ArrayTypeConverter.Instance.Serialize(expected, typeof(int[,]), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            var actual = (Array)ArrayTypeConverter.Instance.Deserialize(typeof(int[,]), reader);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DeserializeICollection_Generic()
        {
            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());

            var expected = new Dictionary<int, int>() { { 1, 2 }, { 3, 4 } };

            CollectionTypeConverter.Instance.Serialize(expected, typeof(Dictionary<int, int>), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            var actual = (Dictionary<int, int>)CollectionTypeConverter.Instance.Deserialize(typeof(Dictionary<int, int>), reader);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DeserializeIList_NonGeneric()
        {
            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());

            var expected = new ArrayList() { 1, 2, 3, 4 };

            CollectionTypeConverter.Instance.Serialize(expected, typeof(ArrayList), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            var actual = (ArrayList)CollectionTypeConverter.Instance.Deserialize(typeof(ArrayList), reader);

            CollectionAssert.AreEqual(expected, actual);
        }


        [TestMethod]
        public void DeserializeIList_NonGeneric_Map()
        {
            var map = new CollectionMapItem(false, typeof(int), CollectionInfo.NonGenericIList, new TypeConverterMapItem(false, NumberTypeConverter.Instance));
            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());

            var expected = new ArrayList() { 1, 2, 3, 4 };

            map.Serialize(expected, typeof(ArrayList), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            var actual = (ArrayList)map.Deserialize(typeof(ArrayList), reader);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DeserializeIDictionary_Generic()
        {
            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());

            var expected = new Dictionary<string, int>() { { "First", 1 }, { "Second", 2 } };

            CollectionTypeConverter.Instance.Serialize(expected, typeof(Dictionary<string, int>), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            var actual = (Dictionary<string, int>)CollectionTypeConverter.Instance.Deserialize(typeof(Dictionary<string, int>), reader);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DeserializeIDictionary_NonGeneric()
        {
            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());

            var expected = new Hashtable() { { "First", 1 }, { "Second", 2 } };

            CollectionTypeConverter.Instance.Serialize(expected, typeof(Hashtable), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            var actual = (Hashtable)CollectionTypeConverter.Instance.Deserialize(typeof(Hashtable), reader);

            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
