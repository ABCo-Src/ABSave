using ABCo.ABSave.Configuration;
using ABCo.ABSave.Mapping.Description.Attributes;
using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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
                Serializer.WriteItem(1, CurrentMapItem);
                AssertAndGoToStart(0, BaseTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(55, Deserializer.ReadItem(CurrentMapItem));

                ClearStream();

                // Without version
                Serializer.WriteItem(1, CurrentMapItem);
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
                Serializer.WriteItem(1, CurrentMapItem);
                AssertAndGoToStart(0, 128, BaseTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(55, Deserializer.ReadItem(CurrentMapItem));

                ClearStream();

                // Without version
                Serializer.WriteItem(1, CurrentMapItem);
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
                Serializer.WriteItem(new BaseIndex(), CurrentMapItem);
                AssertAndGoToStart(0x80, 0x80, BaseTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(55, Deserializer.ReadItem(CurrentMapItem));

                ClearStream();

                // Without version
                Serializer.WriteItem(new BaseIndex(), CurrentMapItem);
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
                Serializer.WriteItem(new BaseIndex(), CurrentMapItem);
                AssertAndGoToStart(0x80, 0xC0, BaseTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(55, Deserializer.ReadItem(CurrentMapItem));

                ClearStream();

                // Without version
                Serializer.WriteItem(new BaseIndex(), CurrentMapItem);
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
                Serializer.WriteItem(new ClassWithMinVersion(), CurrentMapItem);
                AssertAndGoToStart(129, 128, OtherTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(ClassWithMinVersion));

                ClearStream();

                // Without version
                Serializer.WriteItem(new ClassWithMinVersion(), CurrentMapItem);
                AssertAndGoToStart(0xC0, OtherTypeConverter.OUTPUT_BYTE);

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
                Serializer.WriteItem(new ClassWithMinVersion(), CurrentMapItem);
                AssertAndGoToStart(129, OtherTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(ClassWithMinVersion));

                ClearStream();

                // Without version
                Serializer.WriteItem(new ClassWithMinVersion(), CurrentMapItem);
                AssertAndGoToStart(128, OtherTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(ClassWithMinVersion));
            }
        }

        [TestMethod]
        public void MatchingRef_WithHeader_CustomVersion()
        {
            OtherTypeConverter.WritesToHeader = true;
            ResetStateWithMapFor<ClassWithMinVersion>();
            {
                Serializer = CurrentMap.GetSerializer(Stream, true, new Dictionary<System.Type, uint>() { { typeof(ClassWithMinVersion), 0 } });

                // With version
                Serializer.WriteItem(new ClassWithMinVersion(), CurrentMapItem);
                AssertAndGoToStart(128, 192, OtherTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(ClassWithMinVersion));

                ClearStream();

                // Without version
                Serializer.WriteItem(new ClassWithMinVersion(), CurrentMapItem);
                AssertAndGoToStart(224, OtherTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(ClassWithMinVersion));
            }
        }

        [TestMethod]
        public void MatchingRef_WithHeader_DoNotWriteVersion()
        {
            Initialize(ABSaveSettings.ForSpeed);

            OtherTypeConverter.WritesToHeader = true;
            ResetStateWithMapFor<ClassWithMinVersion>();
            {
                Serializer = CurrentMap.GetSerializer(Stream, false);
                Deserializer = CurrentMap.GetDeserializer(Stream, false);

                // Once with cache, the other without cache
                for (int i = 0; i < 2; i++)
                {
                    Serializer.WriteItem(new ClassWithMinVersion(), CurrentMapItem);
                    AssertAndGoToStart(0xE0, OtherTypeConverter.OUTPUT_BYTE);

                    Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(ClassWithMinVersion));
                    ClearStream();
                }
            }
        }

        [TestMethod]
        public void MatchingRef_WithoutHeader_CustomVersion()
        {
            OtherTypeConverter.WritesToHeader = false;
            ResetStateWithMapFor<ClassWithMinVersion>();
            {
                Serializer = CurrentMap.GetSerializer(Stream, true, new Dictionary<System.Type, uint>() { { typeof(ClassWithMinVersion), 0 } });

                // With version
                Serializer.WriteItem(new ClassWithMinVersion(), CurrentMapItem);
                AssertAndGoToStart(0x80, 0x80, OtherTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(ClassWithMinVersion));

                ClearStream();

                // Without version
                Serializer.WriteItem(new ClassWithMinVersion(), CurrentMapItem);
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
                Serializer = CurrentMap.GetSerializer(Stream, false);
                Deserializer = CurrentMap.GetDeserializer(Stream, false);

                // Once with cache, the other without cache
                for (int i = 0; i < 2; i++)
                {
                    Serializer.WriteItem(new ClassWithMinVersion(), CurrentMapItem);
                    AssertAndGoToStart(0xC0, OtherTypeConverter.OUTPUT_BYTE);

                    Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(ClassWithMinVersion));
                    ClearStream();
                }
            }
        }

        [TestMethod]
        public void MatchingRef_Null()
        {
            BaseTypeConverter.WritesToHeader = true;
            ResetStateWithMapFor(typeof(BaseIndex));
            {
                Serializer.WriteItem(null, CurrentMapItem);
                AssertAndGoToStart(0);

                Assert.AreEqual(null, Deserializer.ReadItem(CurrentMapItem));
            }
        }

        [TestMethod]
        public void Nullable_Null()
        {
            BaseTypeConverter.WritesToHeader = true;
            ResetStateWithMapFor(typeof(int?));
            {
                Serializer.WriteItem(null, CurrentMapItem);
                AssertAndGoToStart(0);

                Assert.AreEqual(null, Deserializer.ReadItem(CurrentMapItem));
            }
        }

        [TestMethod]
        public void Nullable_NotNull()
        {
            BaseTypeConverter.WritesToHeader = true;
            ResetStateWithMapFor(typeof(byte?));
            {
                Serializer.WriteItem((byte)5, CurrentMapItem);
                AssertAndGoToStart(0x80, 5);

                Assert.AreEqual((byte)5, Deserializer.ReadItem(CurrentMapItem));
            }
        }

        [TestMethod]
        public void DifferentRef_NoInheritanceInfo()
        {
            ResetStateWithMapFor<NoInheritanceInfoBase>();
            {
                // With version
                Serializer.WriteItem(new NoInheritanceInfoSub(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(new object[] { BitConverter.GetBytes(30) }, 0x80, 0, 0x80, (short)GenType.ByteArr));

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(NoInheritanceInfoBase));

                ClearStream();

                // Without version
                Serializer.WriteItem(new NoInheritanceInfoSub(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(new object[] { BitConverter.GetBytes(30) }, 0xC0, (short)GenType.ByteArr));

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(NoInheritanceInfoBase));
            }
        }

        [TestMethod]
        public void IndexInheritance_WithHeader()
        {
            ResetStateWithMapFor<BaseIndex>();
            {
                // With version
                Serializer.WriteItem(new SubWithHeader(), CurrentMapItem);
                AssertAndGoToStart(128, 2, 0, 128, SubTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(SubWithHeader));

                ClearStream();

                // Without version
                Serializer.WriteItem(new SubWithHeader(), CurrentMapItem);
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
                Serializer.WriteItem(new SubWithoutHeader(), CurrentMapItem);
                AssertAndGoToStart(128, 3, 0, SubTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(SubWithoutHeader));

                ClearStream();

                // Without version
                Serializer.WriteItem(new SubWithoutHeader(), CurrentMapItem);
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
                // With version + no caching
                Serializer.WriteItem(new KeySubSecond(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(new object[] { "Second" }, 128, 6, (short)GenType.String, 0, 128, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(KeySubSecond));

                ClearStream();

                // Without version + caching
                Serializer.WriteItem(new KeySubSecond(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(0xA0, 128, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(KeySubSecond));
            }
        }

        [TestMethod]
        public void KeyInheritance_WithoutHeader()
        {
            OtherTypeConverter.WritesToHeader = false;
            ResetStateWithMapFor<KeyBase>();
            {
                // With version + no caching
                Serializer.WriteItem(new KeySubSecond(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(new object[] { "Second" }, 128, 6, (short)GenType.String, 0, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(KeySubSecond));

                ClearStream();

                // Without version + caching
                Serializer.WriteItem(new KeySubSecond(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(0xA0, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(KeySubSecond));
            }
        }

        [TestMethod]
        public void KeyInheritance_WithoutHeader_MultipleCaches()
        {
            OtherTypeConverter.WritesToHeader = false;
            ResetStateWithMapFor<KeyBase>();
            {
                var keyBase = CurrentMap.GetRuntimeMapItem(typeof(IndexKeyBase));

                // Cache some items
                Serializer.WriteItem(new KeySubSecond(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(new object[] { "Second" }, 128, 6, (short)GenType.String, 0, OtherTypeConverter.OUTPUT_BYTE));
                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(KeySubSecond));

                ClearStream();

                Serializer.WriteItem(new IndexKeySubKey(), keyBase);
                AssertAndGoToStart(GetByteArr(new object[] { "Key" }, 0x80, 3, (short)GenType.String, 0, OtherTypeConverter.OUTPUT_BYTE));
                Assert.IsInstanceOfType(Deserializer.ReadItem(keyBase), typeof(IndexKeySubKey));

                ClearStream();

                // See if it uses the cached version of those items
                Serializer.WriteItem(new IndexKeySubKey(), keyBase);
                AssertAndGoToStart(GetByteArr(0x91, OtherTypeConverter.OUTPUT_BYTE));
                Assert.IsInstanceOfType(Deserializer.ReadItem(keyBase), typeof(IndexKeySubKey));

                ClearStream();

                Serializer.WriteItem(new KeySubSecond(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(0xA0, OtherTypeConverter.OUTPUT_BYTE));
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
                Serializer.WriteItem(new IndexKeySubIndex(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(128, 64, 0, 128, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(IndexKeySubIndex));

                ClearStream();

                // Without version
                Serializer.WriteItem(new IndexKeySubIndex(), CurrentMapItem);
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
                Serializer.WriteItem(new IndexKeySubIndex(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(128, 64, 0, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(IndexKeySubIndex));

                ClearStream();

                // Without version
                Serializer.WriteItem(new IndexKeySubIndex(), CurrentMapItem);
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
                Serializer.WriteItem(new IndexKeySubKey(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(new object[] { "Key" }, 128, 3, (short)GenType.String, 0, 128, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(IndexKeySubKey));

                ClearStream();

                // Without version
                Serializer.WriteItem(new IndexKeySubKey(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(0x90, 128, OtherTypeConverter.OUTPUT_BYTE));

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
                Serializer.WriteItem(new IndexKeySubKey(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(new object[] { "Key" }, 128, 3, (short)GenType.String, 0, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(IndexKeySubKey));

                ClearStream();

                // Without version
                Serializer.WriteItem(new IndexKeySubKey(), CurrentMapItem);
                AssertAndGoToStart(GetByteArr(0x90, OtherTypeConverter.OUTPUT_BYTE));

                Assert.IsInstanceOfType(Deserializer.ReadItem(CurrentMapItem), typeof(IndexKeySubKey));
            }
        }

        [TestMethod]
        public void Null()
        {
            ResetStateWithMapFor(typeof(NestedClass));
            {
                Serializer.WriteItem(null, CurrentMapItem);
                AssertAndGoToStart(0);

                Assert.AreEqual(null, Deserializer.ReadItem(CurrentMapItem));
            }
        }
    }
}
