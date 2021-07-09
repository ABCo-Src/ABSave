using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

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
        public void ValueType_WithoutHeader()
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
        public void ValueType_WithHeader()
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
        public void MatchingRef_WithoutHeader()
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
        public void MatchingRef_WithHeader()
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
        public void MatchingRef_WithHeader_NonZeroVersion()
        {
            OtherTypeConverter.WritesToHeader = true;
            ResetStateWithMapFor<ClassWithMinVersion>();
            {
                // With version
                Serializer.SerializeItem(new ClassWithMinVersion(), CurrentMapItem);
                AssertAndGoToStart(193, 128, OtherTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(ClassWithMinVersion));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new ClassWithMinVersion(), CurrentMapItem);
                AssertAndGoToStart(224, OtherTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(ClassWithMinVersion));
            }
        }

        [TestMethod]
        public void MatchingRef_WithoutHeader_NonZeroVersion()
        {
            OtherTypeConverter.WritesToHeader = false;
            ResetStateWithMapFor<ClassWithMinVersion>();
            {
                // With version
                Serializer.SerializeItem(new ClassWithMinVersion(), CurrentMapItem);
                AssertAndGoToStart(193, OtherTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(ClassWithMinVersion));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new ClassWithMinVersion(), CurrentMapItem);
                AssertAndGoToStart(192, OtherTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(ClassWithMinVersion));
            }
        }

        [TestMethod]
        public void MatchingRef_WithHeader_CustomVersion()
        {
            OtherTypeConverter.WritesToHeader = true;
            ResetStateWithMapFor<ClassWithMinVersion>();
            {
                Serializer = CurrentMap.GetSerializer(Stream, new Dictionary<System.Type, uint>() { { typeof(ClassWithMinVersion), 0 } });

                // With version
                Serializer.SerializeItem(new ClassWithMinVersion(), CurrentMapItem);
                AssertAndGoToStart(192, 128, OtherTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(ClassWithMinVersion));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new ClassWithMinVersion(), CurrentMapItem);
                AssertAndGoToStart(224, OtherTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(ClassWithMinVersion));
            }
        }

        [TestMethod]
        public void MatchingRef_WithoutHeader_CustomVersion()
        {
            OtherTypeConverter.WritesToHeader = false;
            ResetStateWithMapFor<ClassWithMinVersion>();
            {
                Serializer = CurrentMap.GetSerializer(Stream, new Dictionary<System.Type, uint>() { { typeof(ClassWithMinVersion), 0 } });

                // With version
                Serializer.SerializeItem(new ClassWithMinVersion(), CurrentMapItem);
                AssertAndGoToStart(192, OtherTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(ClassWithMinVersion));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new ClassWithMinVersion(), CurrentMapItem);
                AssertAndGoToStart(192, OtherTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(ClassWithMinVersion));
            }
        }

        [TestMethod]
        public void MatchingRef_Null()
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
        public void IndexInheritance_WithHeader()
        {
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
        public void IndexInheritance_WithoutHeader()
        {
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
        public void KeyInheritance_WithHeader()
        {
            OtherTypeConverter.WritesToHeader = true;
            ResetStateWithMapFor<KeyBase>();
            {
                // With version
                Serializer.SerializeItem(new KeySubSecond(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(new object[] { "Second" }, 128, 6, (short)GenType.String, 0, 128, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(KeySubSecond));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new KeySubSecond(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(new object[] { "Second" }, 134, (short)GenType.String, 128, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(KeySubSecond));
            }
        }

        [TestMethod]
        public void KeyInheritance_WithoutHeader()
        {
            OtherTypeConverter.WritesToHeader = false;
            ResetStateWithMapFor<KeyBase>();
            {
                // With version
                Serializer.SerializeItem(new KeySubSecond(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(new object[] { "Second" }, 128, 6, (short)GenType.String, 0, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(KeySubSecond));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new KeySubSecond(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(new object[] { "Second" }, 134, (short)GenType.String, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(KeySubSecond));
            }
        }

        [TestMethod]
        public void KeyOrIndexInheritance_Index_WithHeader()
        {
            OtherTypeConverter.WritesToHeader = true;
            ResetStateWithMapFor<IndexKeyBase>();
            {
                // With version
                Serializer.SerializeItem(new IndexKeySubIndex(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(128, 128, 0, 128, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(IndexKeySubIndex));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new IndexKeySubIndex(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(160, 128, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(IndexKeySubIndex));
            }
        }

        [TestMethod]
        public void KeyOrIndexInheritance_Index_WithoutHeader()
        {
            OtherTypeConverter.WritesToHeader = false;
            ResetStateWithMapFor<IndexKeyBase>();
            {
                // With version
                Serializer.SerializeItem(new IndexKeySubIndex(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(128, 128, 0, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(IndexKeySubIndex));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new IndexKeySubIndex(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(160, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(IndexKeySubIndex));
            }
        }

        [TestMethod]
        public void KeyOrIndexInheritance_Key_WithHeader()
        {
            OtherTypeConverter.WritesToHeader = true;
            ResetStateWithMapFor<IndexKeyBase>();
            {
                // With version
                Serializer.SerializeItem(new IndexKeySubKey(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(new object[] { "Key" }, 128, 3, (short)GenType.String, 0, 128, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(IndexKeySubKey));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new IndexKeySubKey(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(new object[] { "Key" }, 131, (short)GenType.String, 128, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(IndexKeySubKey));
            }
        }

        [TestMethod]
        public void KeyOrIndexInheritance_Key_WithoutHeader()
        {
            OtherTypeConverter.WritesToHeader = false;
            ResetStateWithMapFor<IndexKeyBase>();
            {
                // With version
                Serializer.SerializeItem(new IndexKeySubKey(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(new object[] { "Key" }, 128, 3, (short)GenType.String, 0, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(IndexKeySubKey));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new IndexKeySubKey(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(new object[] { "Key" }, 131, (short)GenType.String, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(IndexKeySubKey));
            }
        }

        [TestMethod]
        public void Null()
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
