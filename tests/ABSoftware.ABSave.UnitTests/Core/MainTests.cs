using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ABCo.ABSave.UnitTests.Core
{
    [TestClass]
    public class MainTests : TestBase
    {
        [TestInitialize]
        public void Setup()
        {
            Initialize();
        }

        [TestMethod]
        public void Converter_ValueType_WithoutHeader()
        {
            BaseTypeConverter.WritesToHeader = false;
            ResetStateWithMapFor<ConverterValueType>();
            {
                // With version
                Serializer.SerializeItem(1, CurrentMapItem);
                AssertAndGoToStart(0, BaseTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(55, Deserializer.DeserializeItem(CurrentMapItem));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(1, CurrentMapItem);
                AssertAndGoToStart(BaseTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(55, Deserializer.DeserializeItem(CurrentMapItem));
            }
        }

        [TestMethod]
        public void Converter_ValueType_WithHeader()
        {
            BaseTypeConverter.WritesToHeader = true;
            ResetStateWithMapFor<ConverterValueType>();
            {
                // With version
                Serializer.SerializeItem(1, CurrentMapItem);
                AssertAndGoToStart(0, 128, BaseTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(55, Deserializer.DeserializeItem(CurrentMapItem));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(1, CurrentMapItem);
                AssertAndGoToStart(128, BaseTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(55, Deserializer.DeserializeItem(CurrentMapItem));
            }
        }

        [TestMethod]
        public void Converter_MatchingRef_WithoutHeader()
        {
            BaseTypeConverter.WritesToHeader = false;
            ResetStateWithMapFor<BaseIndex>();
            {
                // With version
                Serializer.SerializeItem(new BaseIndex(), CurrentMapItem);
                AssertAndGoToStart(192, BaseTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(55, Deserializer.DeserializeItem(CurrentMapItem));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new BaseIndex(), CurrentMapItem);
                AssertAndGoToStart(192, BaseTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(55, Deserializer.DeserializeItem(CurrentMapItem));
            }
        }

        [TestMethod]
        public void Converter_MatchingRef_WithHeader()
        {
            BaseTypeConverter.WritesToHeader = true;
            ResetStateWithMapFor<BaseIndex>();
            {
                // With version
                Serializer.SerializeItem(new BaseIndex(), CurrentMapItem);
                AssertAndGoToStart(192, 128, BaseTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(55, Deserializer.DeserializeItem(CurrentMapItem));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new BaseIndex(), CurrentMapItem);
                AssertAndGoToStart(224, BaseTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(55, Deserializer.DeserializeItem(CurrentMapItem));
            }
        }

        [TestMethod]
        public void Converter_MatchingRef_Null()
        {
            BaseTypeConverter.WritesToHeader = true;
            ResetStateWithMapFor(typeof(BaseIndex));
            {
                Serializer.SerializeItem(null, CurrentMapItem);
                AssertAndGoToStart(0);

                Assert.AreEqual(null, Deserializer.DeserializeItem(CurrentMapItem));
            }
        }

        [TestMethod]
        public void Item_Converter_DifferentRef_WithHeader()
        {
            BaseTypeConverter.WritesToHeader = true;
            ResetStateWithMapFor<BaseIndex>();
            {
                // With version
                Serializer.SerializeItem(new SubWithHeader(), CurrentMapItem);
                AssertAndGoToStart(128, 2, 0, 128, SubTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(SubWithHeader));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new SubWithHeader(), CurrentMapItem);
                AssertAndGoToStart(130, 128, SubTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(SubWithHeader));
            }
        }

        [TestMethod]
        public void Item_Converter_DifferentRef_WithoutHeader()
        {
            BaseTypeConverter.WritesToHeader = false;
            ResetStateWithMapFor<BaseIndex>();
            {
                // With version
                Serializer.SerializeItem(new SubWithoutHeader(), CurrentMapItem);
                AssertAndGoToStart(128, 3, 0, SubTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(SubWithoutHeader));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new SubWithoutHeader(), CurrentMapItem);
                AssertAndGoToStart(131, SubTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(SubWithoutHeader));
            }
        }

        [TestMethod]
        public void Object_Null()
        {
            ResetStateWithMapFor(typeof(NestedClass));
            {
                Serializer.SerializeItem(null, CurrentMapItem);
                AssertAndGoToStart(0);

                Assert.AreEqual(null, Deserializer.DeserializeItem(CurrentMapItem));
            }
        }

        //[TestMethod]
        //public void Object_ValueType()
        //{
        //    ResetStateWithMapFor(typeof(VerySimpleStruct));
        //    {
        //        Serializer.SerializeItem(new VerySimpleStruct(7, 3), CurrentMapItem);
        //        AssertAndGoToStart(0, 7, 3);

        //        Assert.AreEqual(new VerySimpleStruct(7, 3), Deserializer.DeserializeItem(CurrentMapItem));
        //    }
        //}

        //[TestMethod]
        //public void Object_MatchingRefType()
        //{
        //    ResetStateWithMapFor(typeof(NestedClass));
        //    {
        //        Serializer.SerializeItem(new NestedClass(100), CurrentMapItem);
        //        AssertAndGoToStart(192, 100, 224, SubTypeConverter.OUTPUT_BYTE, 192, SubTypeConverter.OUTPUT_BYTE, 0, 100, 9);

        //        Assert.AreEqual(new NestedClass(100), Deserializer.DeserializeItem(CurrentMapItem));
        //    }
        //}

        //[TestMethod]
        //public void Object_DifferentRefType()
        //{
        //    ResetStateWithMapFor(typeof(BaseIndex));
        //    {
        //        Serializer.SerializeItem(new NestedClass(150), CurrentMapItem);
        //        AssertAndGoToStart(163, 0, 150, 224, SubTypeConverter.OUTPUT_BYTE, 192, SubTypeConverter.OUTPUT_BYTE, 0, 150, 9);

        //        Assert.AreEqual(new NestedClass(150), Deserializer.DeserializeItem(CurrentMapItem));
        //    }
        //}

        //[TestMethod]
        //public void Object_CustomVersion()
        //{
        //    Initialize(ABSaveSettings.ForSpeed, new Dictionary<Type, uint>() { { typeof(VersionedClass), 1 } });
        //    ResetStateWithMapFor(typeof(VersionedClass));
        //    {
        //        var targetObj = new VersionedClass();
        //        Serializer.SerializeItem(targetObj, CurrentMapItem);
        //        AssertAndGoToStart(GetByteArr(new object[] { 3L, 5 }, 193, (short)GenType.Numerical, 1, (short)GenType.Numerical));

        //        Assert.AreEqual(targetObj, Deserializer.DeserializeItem(CurrentMapItem));
        //    }
        //}

        //// When a different type is detected and it changes from a converter to a object or a converter to an object.
        //[TestMethod]
        //public void CrossType_ConvToObj()
        //{
        //    BaseTypeConverter.WritesToHeader = false;
        //    ResetStateWithMapFor<BaseIndex>();
        //    {
        //        Serializer.SerializeItem(new SubNoConverter(150), CurrentMapItem);
        //        AssertAndGoToStart(162, 0, 150);

        //        Assert.AreEqual(new SubNoConverter(150), Deserializer.DeserializeItem(CurrentMapItem));
        //    }
        //}

        //[TestMethod]
        //public void CrossType_ObjToConv()
        //{
        //    ResetStateWithMapFor(typeof(BaseIndex));
        //    {
        //        Serializer.SerializeItem(new SubWithHeader(), CurrentMapItem);
        //        AssertAndGoToStart(160, 128, SubTypeConverter.OUTPUT_BYTE);

        //        Assert.AreEqual(new SubWithHeader(), Deserializer.DeserializeItem(CurrentMapItem));
        //    }
        //}
    }
}
