using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Mapping.Items;
using ABSoftware.ABSave.Serialization;
using ABSoftware.ABSave.Testing.UnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABSoftware.ABSave.Testing.UnitTests.Core
{
    [TestClass]
    public class MainTests : TestBase
    {
        [TestMethod]
        public void Item_Converter_ValueType()
        {
            Initialize();

            // Without header
            ResetOutputWithMapItem(new TestableTypeConverter(false, false).GetMap<int>());
            {
                Serializer.SerializeItem(1, CurrentMapItem);
                AssertAndGoToStart(TestableTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(55, Deserializer.DeserializeItem(CurrentMapItem));
            }

            // With header
            ResetOutputWithMapItem(new TestableTypeConverter(true, false).GetMap<int>());
            {
                Serializer.SerializeItem(1, CurrentMapItem);
                AssertAndGoToStart(128, TestableTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(55, Deserializer.DeserializeItem(CurrentMapItem));
            }
        }

        [TestMethod]
        public void Item_Converter_MatchingRef()
        {
            Initialize();

            // Without header
            ResetOutputWithMapItem(new TestableTypeConverter(false, false).GetMap<Base>());
            {
                Serializer.SerializeItem(new Base(), CurrentMapItem);
                AssertAndGoToStart(192, TestableTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(55, Deserializer.DeserializeItem(CurrentMapItem));
            }

            // With header
            ResetOutputWithMapItem(new TestableTypeConverter(true, false).GetMap<Base>());
            {
                Serializer.SerializeItem(new Base(), CurrentMapItem);
                AssertAndGoToStart(224, TestableTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(55, Deserializer.DeserializeItem(CurrentMapItem));
            }

            // Null
            ResetOutputWithMapItem(new TestableTypeConverter(true, false).GetMap<Base>());
            {
                Serializer.SerializeItem(null, CurrentMapItem);
                AssertAndGoToStart(0);

                Assert.AreEqual(null, Deserializer.DeserializeItem(CurrentMapItem));
            }
        }

        [TestMethod]
        public void Item_Converter_DifferentRef()
        {
            Initialize();

            // DIFFERENT CONVERTER:
            // With header
            ResetOutputWithMapItem(new TestableTypeConverter(false, false).GetMap<Base>());
            {
                Serializer.SerializeItem(new SubWithHeader(), CurrentMapItem);
                AssertAndGoToStart(160, 128, SubTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(SubWithHeader));
            }

            // Without header
            ResetOutputWithMapItem(new TestableTypeConverter(false, false).GetMap<Base>());
            {
                Serializer.SerializeItem(new SubWithoutHeader(), CurrentMapItem);
                AssertAndGoToStart(161, SubTypeConverter.OUTPUT_BYTE);

                Assert.IsInstanceOfType(Deserializer.DeserializeItem(CurrentMapItem), typeof(SubWithoutHeader));
            }

            // SAME CONVERTER:
            // With header
            ResetOutputWithMapItem(new TestableTypeConverter(true, true).GetMap<Base>());
            {
                Serializer.SerializeItem(new SubWithHeader(), CurrentMapItem);
                AssertAndGoToStart(192, TestableTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(TestableTypeConverter.OUTPUT_BYTE, Deserializer.DeserializeItem(CurrentMapItem));
            }

            // Without header
            ResetOutputWithMapItem(new TestableTypeConverter(false, true).GetMap<Base>());
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
            ResetOutputWithMapItem(MapGenerator.Generate(typeof(MyStruct), CurrentMap));
            {
                Serializer.SerializeItem(new MyStruct(7, 3), MapGenerator.Generate(typeof(MyStruct), CurrentMap));
                AssertAndGoToStart(7, 3);

                Assert.AreEqual(new MyStruct(7, 3), Deserializer.DeserializeItem(CurrentMapItem));
            }

            // Null
            ResetOutputWithMapItem(MapGenerator.Generate(typeof(GeneralClass), CurrentMap));
            {
                Serializer.SerializeItem(null, CurrentMapItem);
                AssertAndGoToStart(0);

                Assert.AreEqual(null, Deserializer.DeserializeItem(CurrentMapItem));
            }

            // Matching type
            ResetOutputWithMapItem(MapGenerator.Generate(typeof(GeneralClass), CurrentMap));
            {
                Serializer.SerializeItem(new GeneralClass(100), CurrentMapItem);
                AssertAndGoToStart(192, 100, 224, SubTypeConverter.OUTPUT_BYTE, 192, SubTypeConverter.OUTPUT_BYTE, 100, 9);

                Assert.AreEqual(new GeneralClass(100), Deserializer.DeserializeItem(CurrentMapItem));
            }

            // Different type
            ResetOutputWithMapItem(MapGenerator.Generate(typeof(Base), CurrentMap));
            {
                Serializer.SerializeItem(new GeneralClass(150), CurrentMapItem);
                AssertAndGoToStart(163, 150, 224, SubTypeConverter.OUTPUT_BYTE, 192, SubTypeConverter.OUTPUT_BYTE, 150, 9);

                Assert.AreEqual(new GeneralClass(150), Deserializer.DeserializeItem(CurrentMapItem));
            }
        }

        // When a different type is detected and it changes from a converter to a object or a converter to an object.
        [TestMethod]
        public void Item_CrossType()
        {
            Initialize();

            // Converter to object
            ResetOutputWithMapItem(new TestableTypeConverter(false, false).GetMap(typeof(Base)));
            {
                Serializer.SerializeItem(new SubNoConverter(150), CurrentMapItem);
                AssertAndGoToStart(162, 150);

                Assert.AreEqual(new SubNoConverter(150), Deserializer.DeserializeItem(CurrentMapItem));
            }            

            // Object to converter - With header
            ResetOutputWithMapItem(new ObjectMapItem(Array.Empty<ObjectFieldInfo>(), typeof(object)));
            {
                Serializer.SerializeItem(new SubWithHeader(), CurrentMapItem);
                AssertAndGoToStart(160, 128, SubTypeConverter.OUTPUT_BYTE);

                Assert.AreEqual(new SubWithHeader(), Deserializer.DeserializeItem(CurrentMapItem));
            }
            
        }
    }
}
