//using ABSoftware.ABSave.Converters;
//using ABSoftware.ABSave.Exceptions;
//using ABSoftware.ABSave.UnitTests.Serialization;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Diagnostics.CodeAnalysis;
//using System.Text;

//namespace ABSoftware.ABSave.UnitTests
//{
//    [TestClass]
//    public class CollectionHandlingTests
//    {
//        [TestMethod]
//        public void GetCollectionInfo_GenericICollection_NonGenericIList()
//        {
//            var details = EnumerableTypeConverter.Instance.GetCollectionDetails(typeof(List<string>));

//            Assert.AreEqual(typeof(NonGenericIListInfo), details.Info.GetType());
//            Assert.AreEqual(typeof(string), details.ElementTypeOrKeyType);
//        }

//        [TestMethod]
//        public void GetCollectionInfo_GenericICollection()
//        {
//            var details = EnumerableTypeConverter.Instance.GetCollectionDetails(typeof(GenericICollection));

//            Assert.AreEqual(typeof(GenericICollectionInfo), details.Info.GetType());
//            Assert.AreEqual(typeof(string), details.ElementTypeOrKeyType);
//        }

//        [TestMethod]
//        public void GetCollectionInfo_NonGenericIList()
//        {
//            var details = EnumerableTypeConverter.Instance.GetCollectionDetails(typeof(ArrayList));

//            Assert.AreEqual(typeof(NonGenericIListInfo), details.Info.GetType());
//            Assert.AreEqual(typeof(object), details.ElementTypeOrKeyType);
//        }

//        [TestMethod]
//        public void GetCollectionInfo_GenericIDictionary_NonGenericIDictionary()
//        {
//            var details = EnumerableTypeConverter.Instance.GetCollectionDetails(typeof(Dictionary<string, string>));

//            Assert.AreEqual(typeof(NonGenericIDictionaryInfo), details.Info.GetType());
//            Assert.AreEqual(typeof(string), details.ElementTypeOrKeyType);
//            Assert.AreEqual(typeof(string), details.ValueType);
//        }

//        [TestMethod]
//        public void GetCollectionInfo_GenericIDictionary()
//        {
//            var details = EnumerableTypeConverter.Instance.GetCollectionDetails(typeof(GenericIDictionary));

//            Assert.AreEqual(typeof(GenericIDictionaryInfo), details.Info.GetType());
//            Assert.AreEqual(typeof(string), details.ElementTypeOrKeyType);
//            Assert.AreEqual(typeof(int), details.ValueType);
//        }

//        [TestMethod]
//        public void GetCollectionInfo_NonGenericIDictionary()
//        {
//            var details = EnumerableTypeConverter.Instance.GetCollectionDetails(typeof(Hashtable));

//            Assert.AreEqual(typeof(NonGenericIDictionaryInfo), details.Info.GetType());
//            Assert.AreEqual(typeof(object), details.ElementTypeOrKeyType);
//            Assert.AreEqual(typeof(object), details.ValueType);
//        }

//        [TestMethod]
//        public void GetCollectionInfo_None()
//        {
//            try
//            {
//                var result = EnumerableTypeConverter.Instance.GetCollectionDetails(typeof(CollectionHandlingTests));
//            }
//            catch (ABSaveUnrecognizedCollectionException) { return; }

//            throw new Exception("Exception was not thrown!");
//        }

//        class GenericICollection : ICollection<string>
//        {
//            public int Count => throw new NotImplementedException();
//            public bool IsReadOnly => throw new NotImplementedException();
//            public void Add(string item) => throw new NotImplementedException();
//            public void Clear() => throw new NotImplementedException();
//            public bool Contains(string item) => throw new NotImplementedException();
//            public void CopyTo(string[] array, int arrayIndex) => throw new NotImplementedException();
//            public IEnumerator<string> GetEnumerator() => throw new NotImplementedException();
//            public bool Remove(string item) => throw new NotImplementedException();
//            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
//        }

//        class GenericIDictionary : IDictionary<string, int>
//        {
//            public int this[string key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
//            public ICollection<string> Keys => throw new NotImplementedException();
//            public ICollection<int> Values => throw new NotImplementedException();
//            public int Count => throw new NotImplementedException();
//            public bool IsReadOnly => throw new NotImplementedException();
//            public void Add(string key, int value) => throw new NotImplementedException();
//            public void Add(KeyValuePair<string, int> item) => throw new NotImplementedException();
//            public void Clear() => throw new NotImplementedException();
//            public bool Contains(KeyValuePair<string, int> item) => throw new NotImplementedException();
//            public bool ContainsKey(string key) => throw new NotImplementedException();
//            public void CopyTo(KeyValuePair<string, int>[] array, int arrayIndex) => throw new NotImplementedException();
//            public IEnumerator<KeyValuePair<string, int>> GetEnumerator() => throw new NotImplementedException();
//            public bool Remove(string key) => throw new NotImplementedException();
//            public bool Remove(KeyValuePair<string, int> item) => throw new NotImplementedException();
//            public bool TryGetValue(string key, [MaybeNullWhen(false)] out int value) => throw new NotImplementedException();
//            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
//        }
//    }
//}
