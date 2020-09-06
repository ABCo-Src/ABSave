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
        public void GenericICollectionWrapper()
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
        public void NonGenericIListWrapper()
        {
            var genericWrapper = new NonGenericIListWrapper();

            genericWrapper.SetCollection(new ArrayList() { "abc" });
            Assert.IsTrue(typeof(object).IsEquivalentTo(genericWrapper.ElementType));
            Assert.AreEqual(1, genericWrapper.Count);

            var enumerator = genericWrapper.GetEnumerator();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual("abc", enumerator.Current);
            Assert.IsFalse(enumerator.MoveNext());

            genericWrapper.CreateCollection(1, typeof(ArrayList));
            genericWrapper.AddItem("abc");
            Assert.AreEqual(1, genericWrapper.Count);
        }
    }
}
