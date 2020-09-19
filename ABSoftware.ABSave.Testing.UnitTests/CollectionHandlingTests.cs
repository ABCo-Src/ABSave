using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Exceptions;
using ABSoftware.ABSave.Testing.UnitTests.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Testing.UnitTests
{
    [TestClass]
    public class CollectionHandlingTests
    {

        [TestMethod]
        public void GetCollectionWrapper_GenericICollection()
        {
            var info = CollectionTypeConverter.Instance.GetCollectionInfo(typeof(List<string>), out Type elementType);

            Assert.AreEqual(typeof(GenericICollectionInfo), info.GetType());
            Assert.AreEqual(typeof(string), elementType);
        }

        [TestMethod]
        public void GetCollectionWrapper_NonGenericIList()
        {
            var info = CollectionTypeConverter.Instance.GetCollectionInfo(typeof(ArrayList), out Type elementType);

            Assert.AreEqual(typeof(NonGenericIListInfo), info.GetType());
            Assert.AreEqual(typeof(object), elementType);
        }

        [TestMethod]
        public void GetCollectionWrapper_GenericIDictionary()
        {
            var info = CollectionTypeConverter.Instance.GetCollectionInfo(typeof(Dictionary<string, string>), out Type elementType);

            Assert.AreEqual(typeof(GenericIDictionaryInfo), info.GetType());
            Assert.AreEqual(typeof(KeyValuePair<string, string>), elementType);
        }

        [TestMethod]
        public void GetCollectionWrapper_NonGenericIDictionary()
        {
            var info = CollectionTypeConverter.Instance.GetCollectionInfo(typeof(Hashtable), out Type elementType);

            Assert.AreEqual(typeof(NonGenericIDictionaryInfo), info.GetType());
            Assert.AreEqual(typeof(DictionaryEntry), elementType);
        }

        [TestMethod]
        public void GetCollectionWrapper_None()
        {
            try
            {
                var result = CollectionTypeConverter.Instance.GetCollectionInfo(typeof(CollectionSerializerTests), out Type elementType);
            }
            catch (ABSaveUnrecognizedCollectionException) { return; }

            throw new Exception("Exception was not thrown!");
        }
    }
}
