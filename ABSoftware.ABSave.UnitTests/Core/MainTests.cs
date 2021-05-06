using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Serialization;
using ABSoftware.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABSoftware.ABSave.UnitTests.Core
{
    [TestClass]
    public class MainTests : TestBase
    {
        void Setup(bool isInheritanceEnabled)
        {
            Initialize(ABSaveSettings.GetSizeFocus(isInheritanceEnabled));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Item_Converter_ValueType(bool isInheritanceEnabled)
        {
            Setup(isInheritanceEnabled);

            // Without header
            ResetStateWithConverter<int>(new TestableTypeConverter(false, false));
            {
                Serializer.SerializeItem(1, CurrentMapItem);
                AssertAndGoToStart(TestableTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(55, Deserializer.DeserializeItem(CurrentMapItem));
            }

            // With header
            ResetStateWithConverter<int>(new TestableTypeConverter(true, false));
            {
                Serializer.SerializeItem(1, CurrentMapItem);
                AssertAndGoToStart(128, TestableTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(55, Deserializer.DeserializeItem(CurrentMapItem));
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Item_Converter_MatchingRef(bool isInheritanceEnabled)
        {
            Setup(isInheritanceEnabled);

            // Without header
            ResetStateWithConverter<Base>(new TestableTypeConverter(false, false));
            {
                Serializer.SerializeItem(new Base(), CurrentMapItem);
                AssertAndGoToStart(isInheritanceEnabled ? (byte)192 : (byte)128, TestableTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(55, Deserializer.DeserializeItem(CurrentMapItem));
            }

            // With header
            ResetStateWithConverter<Base>(new TestableTypeConverter(true, false));
            {
                Serializer.SerializeItem(new Base(), CurrentMapItem);
                AssertAndGoToStart(isInheritanceEnabled ? (byte)224 : (byte)192, TestableTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(55, Deserializer.DeserializeItem(CurrentMapItem));
            }

            // Null
            ResetStateWithConverter<Base>(new TestableTypeConverter(true, false));
            {
                Serializer.SerializeItem(null, CurrentMapItem);
                AssertAndGoToStart(0);

                Assert.AreEqual(null, Deserializer.DeserializeItem(CurrentMapItem));
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Item_Converter_DifferentRef(bool isInheritanceEnabled)
        {
            Setup(isInheritanceEnabled);

            // DIFFERENT CONVERTER:
            // With header
            if (isInheritanceEnabled)
            {
                ResetStateWithConverter<Base>(new TestableTypeConverter(false, false));
                {
                    Serializer.SerializeItem(new SubWithHeader(), CurrentMapItem);
                    AssertAndGoToStart(160, 128, SubTypeConverter.OUTPUT_BYTE);

                    Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(SubWithHeader));
                }

                // Without header
                ResetStateWithConverter<Base>(new TestableTypeConverter(false, false));
                {
                    Serializer.SerializeItem(new SubWithoutHeader(), CurrentMapItem);
                    AssertAndGoToStart(161, SubTypeConverter.OUTPUT_BYTE);

                    Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(SubWithoutHeader));
                }
            }

            // SAME CONVERTER:
            // With header
            ResetStateWithConverter<Base>(new TestableTypeConverter(true, true));
            {
                Serializer.SerializeItem(new SubWithHeader(), CurrentMapItem);
                AssertAndGoToStart(192, TestableTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(TestableTypeConverter.OUTPUT_BYTE, Deserializer.DeserializeItem(CurrentMapItem));
            }

            // Without header
            ResetStateWithConverter<Base>(new TestableTypeConverter(false, true));
            {
                Serializer.SerializeItem(new SubWithoutHeader(), CurrentMapItem);
                AssertAndGoToStart(128, TestableTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(TestableTypeConverter.OUTPUT_BYTE, Deserializer.DeserializeItem(CurrentMapItem));
            }
        }

        [TestMethod]
        public void Item_Object()
        {
            Initialize();

            // Value type
            ResetStateWithMapFor(typeof(MyStruct));
            {
                Serializer.SerializeItem(new MyStruct(7, 3), CurrentMapItem);
                AssertAndGoToStart(0, 7, 3);

                Assert.AreEqual(new MyStruct(7, 3), Deserializer.DeserializeItem(CurrentMapItem));
            }

            // Null
            ResetStateWithMapFor(typeof(GeneralClass));
            {
                Serializer.SerializeItem(null, CurrentMapItem);
                AssertAndGoToStart(0);

                Assert.AreEqual(null, Deserializer.DeserializeItem(CurrentMapItem));
            }

            // Matching type
            ResetStateWithMapFor(typeof(GeneralClass));
            {
                Serializer.SerializeItem(new GeneralClass(100), CurrentMapItem);
                AssertAndGoToStart(192, 100, 224, SubTypeConverter.OUTPUT_BYTE, 192, SubTypeConverter.OUTPUT_BYTE, 0, 100, 9);

                Assert.AreEqual(new GeneralClass(100), Deserializer.DeserializeItem(CurrentMapItem));
            }

            // Different type
            ResetStateWithMapFor(typeof(Base));
            {
                Serializer.SerializeItem(new GeneralClass(150), CurrentMapItem);
                AssertAndGoToStart(163, 0, 150, 224, SubTypeConverter.OUTPUT_BYTE, 192, SubTypeConverter.OUTPUT_BYTE, 0, 150, 9);

                Assert.AreEqual(new GeneralClass(150), Deserializer.DeserializeItem(CurrentMapItem));
            }
        }

        // When a different type is detected and it changes from a converter to a object or a converter to an object.
        [TestMethod]
        public void Item_CrossType()
        {
            Initialize();

            // Converter to object
            ResetStateWithConverter<Base>(new TestableTypeConverter(false, false));
            {
                Serializer.SerializeItem(new SubNoConverter(150), CurrentMapItem);
                AssertAndGoToStart(162, 0, 150);

                Assert.AreEqual(new SubNoConverter(150), Deserializer.DeserializeItem(CurrentMapItem));
            }

            // Object to converter - With header
            ResetStateWithMapFor(typeof(Base));
            {
                Serializer.SerializeItem(new SubWithHeader(), CurrentMapItem);
                AssertAndGoToStart(160, 128, SubTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(new SubWithHeader(), Deserializer.DeserializeItem(CurrentMapItem));
            }
        }
    }
}
