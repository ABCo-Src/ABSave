using ABCo.ABSave.Configuration;
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
using ABCo.ABSave.Exceptions;

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
            Assert.IsTrue(obj.DisposedEnumerator);

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
            Assert.IsTrue(obj.DisposedEnumerator);

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
        public void Convert_GenericIDictionary_NonGenericIDictionary()
        {
            Setup<Dictionary<int, bool>>(Settings);

            var obj = new Dictionary<int, bool>() { { 1, true }, { 2, false } };

            DoSerialize(obj);
            AssertAndGoToStart(0, 2, 0, 1, 0, 0x82, 0);

            var deserialized = DoDeserialize<Dictionary<int, bool>>();
            CollectionAssert.AreEqual(obj.Keys.ToList(), deserialized.Keys.ToList());
            CollectionAssert.AreEqual(obj.Values.ToList(), deserialized.Values.ToList());
        }

        [TestMethod]
        public void Convert_GenericIDictionary_NonGenericIDictionary_NullKey()
        {
            Setup<Dictionary<string, int>>(Settings);

            Serializer.WriteByte(0);
            Serializer.WriteByte(1);
            Serializer.WriteByte(0);
            Serializer.WriteByte(0);
            Serializer.WriteByte(5);
            Serializer.Flush();
            GoToStart();

            Assert.ThrowsException<NullDictionaryKeyException>(() => DoDeserialize<Dictionary<string, int>>());
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
            Assert.IsTrue(obj.DisposedEnumerator);
            AssertAndGoToStart(0, 2, 0, 1, 2, 3, 4);

            var deserialized = DoDeserialize<GenericIDictionary>();
            CollectionAssert.AreEqual(obj.Keys.ToList(), deserialized.Keys.ToList());
            CollectionAssert.AreEqual(obj.Values.ToList(), deserialized.Values.ToList());
        }
        
        [TestMethod]
        public void Convert_GenericIDictionary_InvalidEnumerator()
        {
            Setup<GenericIDictionary>(Settings);

            var obj = new GenericIDictionary(true) { { 1, 2 }, { 3, 4 } };

            Assert.ThrowsException<InvalidDictionaryException>(() => DoSerialize(obj));
            Assert.IsTrue(obj.DisposedEnumerator);
        }

        public class GenericAndNonGeneric : ICollection<int>, IList
        {
            public bool DisposedEnumerator { get; set; }

            readonly List<int> _inner = new List<int>();

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
            public IEnumerator<int> GetEnumerator() => new DisposalNotificationEnumerator<int>(this, _inner.GetEnumerator());
            public int IndexOf(object value) => _inner.IndexOf((int)value);
            public void Insert(int index, object value) => _inner.Insert(index, (int)value);
            public bool Remove(int item) => _inner.Remove(item);
            public void Remove(object value) => _inner.Remove((int)value);
            public void RemoveAt(int index) => _inner.RemoveAt(index);
            IEnumerator IEnumerable.GetEnumerator() => new DisposalNotificationEnumerator<int>(this, ((IEnumerable)_inner).GetEnumerator());
        }

        public class GenericICollection : ICollection<int>
        {
            public bool DisposedEnumerator { get; set; }

            public List<int> Inner = new List<int>();

            public int Count => Inner.Count;
            public bool IsReadOnly => false;
            public void Add(int item) => Inner.Add(item);
            public void Clear() => Inner.Clear();
            public bool Contains(int item) => Inner.Contains(item);
            public void CopyTo(int[] array, int arrayIndex) => Inner.CopyTo(array, arrayIndex);
            public IEnumerator<int> GetEnumerator() => new DisposalNotificationEnumerator<int>(this, Inner.GetEnumerator());
            public bool Remove(int item) => Inner.Remove(item);
            IEnumerator IEnumerable.GetEnumerator() => new DisposalNotificationEnumerator<int>(this, ((IEnumerable)Inner).GetEnumerator());
        }

        public class GenericIDictionary : IDictionary<int, int>
        {
            public bool DisposedEnumerator { get; set; }

            readonly Dictionary<int, int> _inner = new Dictionary<int, int>();
            readonly bool _useNonDictionaryEnumerator;

            public GenericIDictionary() { }
            public GenericIDictionary(bool useNonDictionaryEnumerator) => _useNonDictionaryEnumerator = useNonDictionaryEnumerator;

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
            public IEnumerator<KeyValuePair<int, int>> GetEnumerator() => _useNonDictionaryEnumerator ? (IEnumerator<KeyValuePair<int, int>>)new NonDictionaryEnumerator(this) : new DisposalNotificationEnumerator<KeyValuePair<int, int>>(this, _inner.GetEnumerator());
            public bool Remove(int key) => _inner.Remove(key);
            public bool Remove(KeyValuePair<int, int> item) => ((IDictionary<int, int>)_inner).Remove(item);    
            public bool TryGetValue(int key, out int value) => _inner.TryGetValue(key, out value);
            IEnumerator IEnumerable.GetEnumerator() => _useNonDictionaryEnumerator ? (IEnumerator)new NonDictionaryEnumerator(this) : new DisposalNotificationEnumerator<KeyValuePair<int, int>>(this, ((IEnumerable)_inner).GetEnumerator());

            class NonDictionaryEnumerator : IEnumerator, IEnumerator<KeyValuePair<int, int>>
            {
                public object Current => 123;

                readonly GenericIDictionary _parent;

                public NonDictionaryEnumerator(GenericIDictionary parent) => _parent = parent;

                KeyValuePair<int, int> IEnumerator<KeyValuePair<int, int>>.Current => new KeyValuePair<int, int>(123, 123);

                public void Dispose() => _parent.DisposedEnumerator = true;
                public bool MoveNext() => throw new Exception("Failed - called 'MoveNext'!");
                public void Reset() => throw new Exception("Failed - called 'Reset'!");
            }
        }

        class DisposalNotificationEnumerator<T> : IEnumerator, IEnumerator<T>, IDictionaryEnumerator
        {
            public object Current => _realEnumerator.Current;
            T IEnumerator<T>.Current => (T)_realEnumerator.Current;

            public DictionaryEntry Entry => ((IDictionaryEnumerator)_realEnumerator).Entry;
            public object Key => ((IDictionaryEnumerator)_realEnumerator).Key;
            public object Value => ((IDictionaryEnumerator)_realEnumerator).Value;

            readonly object _parent;
            readonly IEnumerator _realEnumerator;

            public DisposalNotificationEnumerator(object parent, IEnumerator realEnumerator)
            {
                _parent = parent;
                _realEnumerator = realEnumerator;
            }

            public void Dispose() => ((dynamic)_parent).DisposedEnumerator = true;
            public bool MoveNext() => _realEnumerator.MoveNext();
            public void Reset() => _realEnumerator.Reset();
        }
    }
}
