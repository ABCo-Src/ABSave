﻿using ABCo.ABSave.Configuration;
using ABCo.ABSave.Serialization.Converters;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Generation;
using ABCo.ABSave.Mapping.Generation.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ABCo.ABSave.UnitTests.Converters
{
    [TestClass]
    public class CollectionTests : ConverterTestBase
    {
        static ABSaveSettings Settings = null;
        public MapGenerator CurrentGenerator;

        [TestInitialize]
        public void Setup()
        {
            Settings = ABSaveSettings.ForSize;
            CurrentMap = new ABSaveMap(Settings);
            CurrentGenerator = new MapGenerator();
            CurrentGenerator.Initialize(CurrentMap);
        }

        CollectionConverter InitializeNew(Type type)
        {
            var info = new InitializeInfo(type, CurrentGenerator);

            var converter = new CollectionConverter();
            converter.Initialize(info);

            return converter;
        }

        [TestCleanup]
        public void Cleanup()
        {
            ABSaveMap.ReleaseGenerator(CurrentGenerator);
        }

        [TestMethod]
        public void Context_List()
        {
            var converter = InitializeNew(typeof(List<string>));

            Assert.IsInstanceOfType(converter._info, typeof(ListInfo));
            Assert.AreEqual(typeof(string), converter._elementOrKeyType);
        }

        [TestMethod]
        public void Convert_List()
        {
            Setup<List<byte>>(Settings);

            var obj = new List<byte> { 1, 2, 3, 4 };

            DoSerialize(obj);
            AssertAndGoToStart(0, 4, 0, 1, 2, 3, 4);
            CollectionAssert.AreEqual(obj, DoDeserialize<List<byte>>());
        }

        [TestMethod]
        public void Context_GenericICollection_NonGenericIList()
        {
            var converter = InitializeNew(typeof(GenericAndNonGeneric));

            Assert.IsInstanceOfType(converter._info, typeof(NonGenericIListInfo));
            Assert.AreEqual(typeof(int), converter._elementOrKeyType);
        }

        [TestMethod]
        public void Convert_GenericICollection_NonGenericIList()
        {
            Setup<GenericAndNonGeneric>(Settings);

            var obj = new GenericAndNonGeneric() { 1, 2, 3, 4 };

            DoSerialize(obj);
            AssertAndGoToStart(0, 4, 0, 1, 2, 3, 4);
            CollectionAssert.AreEqual(obj, DoDeserialize<GenericAndNonGeneric>());
        }

        [TestMethod]
        public void Context_GenericICollection()
        {
            var converter = InitializeNew(typeof(GenericICollection));

            Assert.IsInstanceOfType(converter._info, typeof(GenericICollectionInfo));
            Assert.AreEqual(typeof(int), converter._elementOrKeyType);
        }


        [TestMethod]
        public void Convert_GenericICollection()
        {
            Setup<GenericICollection>(Settings);

            var obj = new GenericICollection() { 1, 2, 3, 4 };

            DoSerialize(obj);
            AssertAndGoToStart(0, 4, 0, 1, 2, 3, 4);
            CollectionAssert.AreEqual(obj.Inner, DoDeserialize<GenericICollection>().Inner);
        }

        //[TestMethod]
        //public void Context_NonGenericIList()
        //{
        //    var converter = InitializeNew(typeof(ArrayList));

        //    Assert.IsInstanceOfType(converter._info, typeof(NonGenericIListInfo));
        //    Assert.AreEqual(typeof(object), converter._elementOrKeyType);
        //}

        [TestMethod]
        public void Context_GenericIDictionary_NonGenericIDictionary()
        {
            var converter = InitializeNew(typeof(Dictionary<int, bool>));

            Assert.IsInstanceOfType(converter._info, typeof(NonGenericIDictionaryInfo));
            Assert.AreEqual(typeof(int), converter._elementOrKeyType);
            Assert.AreEqual(typeof(bool), converter._valueType);
        }

        [TestMethod]
        public void Context_GenericIDictionary()
        {
            var converter = InitializeNew(typeof(GenericIDictionary));

            Assert.IsInstanceOfType(converter._info, typeof(GenericIDictionaryInfo));
            Assert.AreEqual(typeof(int), converter._elementOrKeyType);
            Assert.AreEqual(typeof(int), converter._valueType);
        }


        [TestMethod]
        public void Convert_GenericIDictionary()
        {
            Setup<GenericIDictionary>(Settings);

            var obj = new GenericIDictionary() { { 1, 2 }, { 3, 4 } };

            DoSerialize(obj);
            AssertAndGoToStart(0, 2, 0, 1, 2, 3, 4);

            var deserialized = DoDeserialize<GenericIDictionary>();
            CollectionAssert.AreEqual(obj.Keys.ToList(), deserialized.Keys.ToList());
            CollectionAssert.AreEqual(obj.Values.ToList(), deserialized.Values.ToList());
        }


        //        public void SerializeIDictionary_Generic()
        //        {
        //            var actual = new ABSaveSerializer(new MemoryStream(), new ABSaveSettings());
        //            var arrType = typeof(Dictionary<int, int>);

        //            Assert.IsTrue(EnumerableTypeConverter.Instance.TryGenerateContext(arrType));
        //            EnumerableTypeConverter.Instance.Serialize(new Dictionary<int, int>() { { 1, 2 }, { 3, 4 } }, arrType, actual);

        //            var expected = new ABSaveSerializer(new MemoryStream(), new ABSaveSettings());
        //            expected.WriteInt32(2);
        //            expected.WriteInt32(1);
        //            expected.WriteInt32(2);
        //            expected.WriteInt32(3);
        //            expected.WriteInt32(4);

        //            TestUtilities.CompareWriters(expected, actual);
        //        }

        //        [TestMethod]
        //        public void SerializeIList_NonGeneric()
        //        {
        //            var actual = new ABSaveSerializer(new MemoryStream(), new ABSaveSettings());
        //            var arrType = typeof(ArrayList);

        //            Assert.IsTrue(EnumerableTypeConverter.Instance.TryGenerateContext(arrType));
        //            EnumerableTypeConverter.Instance.Serialize(new ArrayList() { 1, 2, 3, 4 }, arrType, actual);

        //            var expected = new ABSaveSerializer(new MemoryStream(), new ABSaveSettings());
        //            expected.WriteInt32(4);
        //            ABSaveItemConverter.Serialize(1, typeof(int), typeof(object), expected);
        //            ABSaveItemConverter.Serialize(2, typeof(int), typeof(object), expected);
        //            ABSaveItemConverter.Serialize(3, typeof(int), typeof(object), expected);
        //            ABSaveItemConverter.Serialize(4, typeof(int), typeof(object), expected);

        //            TestUtilities.CompareWriters(expected, actual);
        //        }

        //        [TestMethod]
        //        public void SerializeIList_NonGeneric_Map()
        //        {
        //            var map = new CollectionMapItem(false, typeof(int), ABSaveCollectionInfo.NonGenericIList, new TypeConverterMapItem(false, NumberTypeConverter.Instance));
        //            var actual = new ABSaveSerializer(new MemoryStream(), new ABSaveSettings());

        //            map.Serialize(new ArrayList() { 1, 2, 3, 4 }, typeof(ArrayList), actual);

        //            var expected = new ABSaveSerializer(new MemoryStream(), new ABSaveSettings());
        //            expected.WriteInt32(4);
        //            expected.WriteInt32(1);
        //            expected.WriteInt32(2);
        //            expected.WriteInt32(3);
        //            expected.WriteInt32(4);

        //            TestUtilities.CompareWriters(expected, actual);
        //        }

        public class GenericAndNonGeneric : ICollection<int>, IList
        {
            List<int> _inner = new List<int>();

            public object this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public int Count => _inner.Count;
            public bool IsReadOnly => false;
            public bool IsFixedSize => false;
            public bool IsSynchronized => false;
            public object SyncRoot => null;
            public void Add(int item) => _inner.Add(item);
            public int Add(object value) => ((IList)_inner).Add(value);
            public void Clear() => _inner.Clear();
            public bool Contains(int item) => _inner.Contains(item);
            public bool Contains(object value) => _inner.Contains((int)value);
            public void CopyTo(int[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);
            public void CopyTo(Array array, int index) => _inner.CopyTo((int[])array, index);
            public IEnumerator<int> GetEnumerator() => _inner.GetEnumerator();
            public int IndexOf(object value) => _inner.IndexOf((int)value);
            public void Insert(int index, object value) => _inner.Insert(index, (int)value);
            public bool Remove(int item) => _inner.Remove(item);
            public void Remove(object value) => _inner.Remove((int)value);
            public void RemoveAt(int index) => _inner.RemoveAt(index);
            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_inner).GetEnumerator();
        }

        public class GenericICollection : ICollection<int>
        {
            public List<int> Inner = new List<int>();

            public int Count => Inner.Count;
            public bool IsReadOnly => false;
            public void Add(int item) => Inner.Add(item);
            public void Clear() => Inner.Clear();
            public bool Contains(int item) => Inner.Contains(item);
            public void CopyTo(int[] array, int arrayIndex) => Inner.CopyTo(array, arrayIndex);
            public IEnumerator<int> GetEnumerator() => Inner.GetEnumerator();
            public bool Remove(int item) => Inner.Remove(item);
            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Inner).GetEnumerator();
        }

        public class GenericIDictionary : IDictionary<int, int>
        {
            Dictionary<int, int> _inner = new Dictionary<int, int>();

            public int this[int key] { get => _inner[key]; set => _inner[key] = value; }
            public ICollection<int> Keys => _inner.Keys;
            public ICollection<int> Values => _inner.Values;
            public int Count => _inner.Count;
            public bool IsReadOnly => false;
            public void Add(int key, int value) => _inner.Add(key, value);
            public void Add(KeyValuePair<int, int> item) => ((IDictionary<int, int>)_inner).Add(item);
            public void Clear() => _inner.Clear();
            public bool Contains(KeyValuePair<int, int> item) => ((IDictionary<int, int>)_inner).Contains(item);
            public bool ContainsKey(int key) => _inner.ContainsKey(key);
            public void CopyTo(KeyValuePair<int, int>[] array, int arrayIndex) => ((IDictionary<int, int>)_inner).CopyTo(array, arrayIndex);
            public IEnumerator<KeyValuePair<int, int>> GetEnumerator() => _inner.GetEnumerator();
            public bool Remove(int key) => _inner.Remove(key);
            public bool Remove(KeyValuePair<int, int> item) => ((IDictionary<int, int>)_inner).Remove(item);
            public bool TryGetValue(int key, out int value) => _inner.TryGetValue(key, out value);
            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_inner).GetEnumerator();
        }
    }
}
