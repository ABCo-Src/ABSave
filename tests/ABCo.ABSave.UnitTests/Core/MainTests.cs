using ABCo.ABSave.Configuration;
using ABCo.ABSave.Mapping.Description.Attributes;
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

                Assert.AreEqual(55, Deserializer.ReadItem(CurrentMapItem));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(1, CurrentMapItem);
                AssertAndGoToStart(BaseTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(55, Deserializer.ReadItem(CurrentMapItem));
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

                Assert.AreEqual(55, Deserializer.ReadItem(CurrentMapItem));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(1, CurrentMapItem);
                AssertAndGoToStart(128, BaseTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(55, Deserializer.ReadItem(CurrentMapItem));
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

                Assert.AreEqual(55, Deserializer.ReadItem(CurrentMapItem));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new BaseIndex(), CurrentMapItem);
                AssertAndGoToStart(192, BaseTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(55, Deserializer.ReadItem(CurrentMapItem));
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

                Assert.AreEqual(55, Deserializer.ReadItem(CurrentMapItem));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new BaseIndex(), CurrentMapItem);
                AssertAndGoToStart(224, BaseTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(55, Deserializer.ReadItem(CurrentMapItem));
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

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(ClassWithMinVersion));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new ClassWithMinVersion(), CurrentMapItem);
                AssertAndGoToStart(224, OtherTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(ClassWithMinVersion));
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

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(ClassWithMinVersion));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new ClassWithMinVersion(), CurrentMapItem);
                AssertAndGoToStart(192, OtherTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(ClassWithMinVersion));
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

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(ClassWithMinVersion));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new ClassWithMinVersion(), CurrentMapItem);
                AssertAndGoToStart(224, OtherTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(ClassWithMinVersion));
            }
        }

        [TestMethod]
        public void MatchingRef_WithHeader_DoNotWriteVersion()
        {
            Initialize(ABSaveSettings.ForSpeed.Customize(b => b.SetIncludeVersioning(false)));

            OtherTypeConverter.WritesToHeader = true;
            ResetStateWithMapFor<ClassWithMinVersion>();
            {
                Serializer = CurrentMap.GetSerializer(Stream);

                // Once with cache, the other without cache
                for (int i = 0; i < 2; i++)
                {
                    Serializer.SerializeItem(new ClassWithMinVersion(), CurrentMapItem);
                    AssertAndGoToStart(0xE0, OtherTypeConverter.OUTPUT_BYTE);

                    Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(ClassWithMinVersion));
                    ResetPosition();
                }
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

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(ClassWithMinVersion));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new ClassWithMinVersion(), CurrentMapItem);
                AssertAndGoToStart(192, OtherTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(ClassWithMinVersion));
            }
        }

        [TestMethod]
        public void MatchingRef_WithoutHeader_DoNotWriteVersion()
        {
            Initialize(ABSaveSettings.ForSpeed);

            OtherTypeConverter.WritesToHeader = false;
            ResetStateWithMapFor<ClassWithMinVersion>();
            {
                Serializer = CurrentMap.GetSerializer(Stream);

                // Once with cache, the other without cache
                for (int i = 0; i < 2; i++)
                {
                    Serializer.SerializeItem(new ClassWithMinVersion(), CurrentMapItem);
                    AssertAndGoToStart(0xC0, OtherTypeConverter.OUTPUT_BYTE);

                    Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(ClassWithMinVersion));
                    ResetPosition();
                }
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

                Assert.AreEqual(null, Deserializer.ReadItem(CurrentMapItem));
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

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(SubWithHeader));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new SubWithHeader(), CurrentMapItem);
                AssertAndGoToStart(130, 128, SubTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(SubWithHeader));
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

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(SubWithoutHeader));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new SubWithoutHeader(), CurrentMapItem);
                AssertAndGoToStart(131, SubTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(SubWithoutHeader));
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

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(KeySubSecond));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new KeySubSecond(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(new object[] { "Second" }, 134, (short)GenType.String, 128, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(KeySubSecond));
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

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(KeySubSecond));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new KeySubSecond(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(new object[] { "Second" }, 134, (short)GenType.String, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(KeySubSecond));
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

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(IndexKeySubIndex));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new IndexKeySubIndex(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(160, 128, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(IndexKeySubIndex));
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

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(IndexKeySubIndex));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new IndexKeySubIndex(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(160, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(IndexKeySubIndex));
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

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(IndexKeySubKey));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new IndexKeySubKey(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(new object[] { "Key" }, 131, (short)GenType.String, 128, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(IndexKeySubKey));
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

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(IndexKeySubKey));

                ResetPosition();

                // Without version
                Serializer.SerializeItem(new IndexKeySubKey(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(new object[] { "Key" }, 131, (short)GenType.String, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(IndexKeySubKey));
            }
        }

        [TestMethod]
        public void Null()
        {
            ResetStateWithMapFor(typeof(NestedClass));
            {
                Serializer.SerializeItem(null, CurrentMapItem);
                AssertAndGoToStart(0);

                Assert.AreEqual(null, Deserializer.ReadItem(CurrentMapItem));
            }
        }
    }
}
