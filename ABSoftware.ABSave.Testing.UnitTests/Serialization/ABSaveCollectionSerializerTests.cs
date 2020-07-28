using ABSoftware.ABSave.Converters.Internal;
using ABSoftware.ABSave.Helpers;
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
        public void SerializeArray_OneDimensional()
        {
            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            var arrType = new TypeInformation(typeof(int[]), TypeCode.Object);

            Assert.IsTrue(CollectionTypeConverter.Instance.CheckCanConvertType(arrType));
            CollectionTypeConverter.Instance.Serialize(new int[] { 1, 2, 3, 4 }, arrType, actual);

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteInt32(4);
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
