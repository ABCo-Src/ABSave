using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Mapping.Generation;
using ABSoftware.ABSave.Serialization;
using ABSoftware.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ABSoftware.ABSave.UnitTests.Converters
{
    [TestClass]
    public class CollectionTests : ConverterTestBase
    {
        static ABSaveSettings Settings = null!;
        public MapGenerator CurrentGenerator;
        ContextGen ContextGenInfo = new();

        [TestInitialize]
        public void Setup()
        {
            var builder = new ABSaveSettingsBuilder
            {
                BypassDangerousTypeChecking = true
            };

            Settings = builder.CreateSettings(ABSaveSettings.ForSize);
            CurrentMap = new ABSaveMap(Settings);
            CurrentGenerator = new MapGenerator();
            CurrentGenerator.Initialize(CurrentMap);

            ContextGenInfo = new ContextGen(typeof(object), CurrentGenerator);
        }

        [TestCleanup]
        public void Cleanup()
        {
            CurrentMap.ReleaseGenerator(CurrentGenerator);
        }

        [TestMethod]
        public void Context_List()
        {
            ContextGenInfo.Type = typeof(List<string>);
            EnumerableConverter.Instance.TryGenerateContext(ref ContextGenInfo);

            var ctx = (EnumerableConverter.Context)ContextGenInfo.ContextInstance!;

            Assert.IsInstanceOfType(ctx.Info, typeof(ListInfo));
            Assert.AreEqual(typeof(string), ctx.ElementOrKeyType);
        }

        [TestMethod]
        public void Context_GenericICollection_NonGenericIList()
        {
            ContextGenInfo.Type = typeof(GenericAndNonGeneric);
            EnumerableConverter.Instance.TryGenerateContext(ref ContextGenInfo);

            var ctx = (EnumerableConverter.Context)ContextGenInfo.ContextInstance!;

            Assert.IsInstanceOfType(ctx.Info, typeof(NonGenericIListInfo));
            Assert.AreEqual(typeof(string), ctx.ElementOrKeyType);
        }

        [TestMethod]
        public void Context_GenericICollection()
        {
            ContextGenInfo.Type = typeof(GenericICollection);
            EnumerableConverter.Instance.TryGenerateContext(ref ContextGenInfo);

            var ctx = (EnumerableConverter.Context)ContextGenInfo.ContextInstance!;

            Assert.IsInstanceOfType(ctx.Info, typeof(GenericICollectionInfo));
            Assert.AreEqual(typeof(int), ctx.ElementOrKeyType);
        }

        [TestMethod]
        public void Context_NonGenericIList()
        {
            ContextGenInfo.Type = typeof(ArrayList);
            EnumerableConverter.Instance.TryGenerateContext(ref ContextGenInfo);

            var ctx = (EnumerableConverter.Context)ContextGenInfo.ContextInstance!;

            Assert.IsInstanceOfType(ctx.Info, typeof(NonGenericIListInfo));
            Assert.AreEqual(typeof(object), ctx.ElementOrKeyType);
        }

        [TestMethod]
        public void Context_GenericIDictionary_NonGenericIDictionary()
        {
            ContextGenInfo.Type = typeof(Dictionary<int, bool>);
            EnumerableConverter.Instance.TryGenerateContext(ref ContextGenInfo);

            var ctx = (EnumerableConverter.Context)ContextGenInfo.ContextInstance!;

            Assert.IsInstanceOfType(ctx.Info, typeof(NonGenericIDictionaryInfo));
            Assert.AreEqual(typeof(int), ctx.ElementOrKeyType);
            Assert.AreEqual(typeof(bool), ctx.ValueType);
        }

        [TestMethod]
        public void Context_GenericIDictionary()
        {
            ContextGenInfo.Type = typeof(GenericIDictionary);
            EnumerableConverter.Instance.TryGenerateContext(ref ContextGenInfo);

            var ctx = (EnumerableConverter.Context)ContextGenInfo.ContextInstance!;

            Assert.IsInstanceOfType(ctx.Info, typeof(GenericIDictionaryInfo));
            Assert.AreEqual(typeof(string), ctx.ElementOrKeyType);
            Assert.AreEqual(typeof(int), ctx.ValueType);
        }

        [TestMethod]
        public void Context_NonGenericIDictionary()
        {
            ContextGenInfo.Type = typeof(Hashtable);
            EnumerableConverter.Instance.TryGenerateContext(ref ContextGenInfo);

            var ctx = (EnumerableConverter.Context)ContextGenInfo.ContextInstance!;

            Assert.IsInstanceOfType(ctx.Info, typeof(NonGenericIDictionaryInfo));
            Assert.AreEqual(typeof(object), ctx.ElementOrKeyType);
            Assert.AreEqual(typeof(object), ctx.ValueType);
        }

        [TestMethod]
        public void Convert_List()
        {
            Setup<List<byte>>(Settings, EnumerableConverter.Instance);

            var obj = new List<byte> { 1, 2, 3, 4 };

            DoSerialize(obj);
            AssertAndGoToStart(4, 1, 2, 3, 4);
            CollectionAssert.AreEqual(obj, DoDeserialize<List<byte>>());
        }

        [TestMethod]
        public void Convert_IDictionary_Generic()
        {
            Setup<Dictionary<byte, byte>>(Settings, EnumerableConverter.Instance);

            var obj = new Dictionary<byte, byte> { { 1, 2 }, { 3, 4 } };

            DoSerialize(obj);
            AssertAndGoToStart(2, 1, 2, 3, 4);
            CollectionAssert.AreEqual(obj, DoDeserialize<Dictionary<byte, byte>>());
        }

        [TestMethod]
        public void Convert_IList_NonGeneric()
        {
            Setup<ArrayList>(Settings, EnumerableConverter.Instance);

            var obj = new ArrayList() { (byte)7 };

            Action<ABSaveSerializer> writeType = s => s.SerializeItem((byte)7, s.GetRuntimeMapItem(typeof(object)));

            DoSerialize(obj);
            AssertAndGoToStart(GetByteArr(new object[] { writeType }, 1, (short)GenType.Action));
            CollectionAssert.AreEqual(obj, DoDeserialize<ArrayList>());
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

        class GenericAndNonGeneric : ICollection<string>, IList
        {
            public object this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public int Count => throw new NotImplementedException();
            public bool IsReadOnly => throw new NotImplementedException();
            public bool IsFixedSize => throw new NotImplementedException();
            public bool IsSynchronized => throw new NotImplementedException();
            public object SyncRoot => throw new NotImplementedException();
            public void Add(string item) => throw new NotImplementedException();
            public int Add(object value) => throw new NotImplementedException();
            public void Clear() => throw new NotImplementedException();
            public bool Contains(string item) => throw new NotImplementedException();
            public bool Contains(object value) => throw new NotImplementedException();
            public void CopyTo(string[] array, int arrayIndex) => throw new NotImplementedException();
            public void CopyTo(Array array, int index) => throw new NotImplementedException();
            public IEnumerator<string> GetEnumerator() => throw new NotImplementedException();
            public int IndexOf(object value) => throw new NotImplementedException();
            public void Insert(int index, object value) => throw new NotImplementedException();
            public bool Remove(string item) => throw new NotImplementedException();
            public void Remove(object value) => throw new NotImplementedException();
            public void RemoveAt(int index) => throw new NotImplementedException();
            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        }

        class GenericICollection : ICollection<int>
        {
            public int Count => throw new NotImplementedException();
            public bool IsReadOnly => throw new NotImplementedException();
            public void Add(int item) => throw new NotImplementedException();
            public void Clear() => throw new NotImplementedException();
            public bool Contains(int item) => throw new NotImplementedException();
            public void CopyTo(int[] array, int arrayIndex) => throw new NotImplementedException();
            public IEnumerator<int> GetEnumerator() => throw new NotImplementedException();
            public bool Remove(int item) => throw new NotImplementedException();
            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        }

        class GenericIDictionary : IDictionary<string, int>
        {
            public int this[string key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public ICollection<string> Keys => throw new NotImplementedException();
            public ICollection<int> Values => throw new NotImplementedException();
            public int Count => throw new NotImplementedException();
            public bool IsReadOnly => throw new NotImplementedException();
            public void Add(string key, int value) => throw new NotImplementedException();
            public void Add(KeyValuePair<string, int> item) => throw new NotImplementedException();
            public void Clear() => throw new NotImplementedException();
            public bool Contains(KeyValuePair<string, int> item) => throw new NotImplementedException();
            public bool ContainsKey(string key) => throw new NotImplementedException();
            public void CopyTo(KeyValuePair<string, int>[] array, int arrayIndex) => throw new NotImplementedException();
            public IEnumerator<KeyValuePair<string, int>> GetEnumerator() => throw new NotImplementedException();
            public bool Remove(string key) => throw new NotImplementedException();
            public bool Remove(KeyValuePair<string, int> item) => throw new NotImplementedException();
            public bool TryGetValue(string key, [MaybeNullWhen(false)] out int value) => throw new NotImplementedException();
            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        }
    }
}
