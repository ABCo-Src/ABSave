using ABCo.ABSave.Configuration;
using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABCo.ABSave.UnitTests.Converters
{
    [TestClass]
    public class ObjectTests : ConverterTestBase
    {
        public enum BaseClass
        {
            None,
            BaseWithHeaderMember,
            BaseWithoutHeaderMember
        }

        //[TestMethod]
        //public void EmptyObject()
        //{
        //    Setup<EmptyClass>(ABSaveSettings.ForSpeed);

        //    DoSerialize(new EmptyClass());
        //    AssertAndGoToStart(0, 0);

        //    Assert.IsInstanceOfType(DoDeserialize<EmptyClass>(), typeof(EmptyClass));
        //}

        //[TestMethod]
        //public void Object_NoBaseMembers()
        //{
        //    Setup<NestedClass>(ABSaveSettings.ForSpeed);

        //    // With version
        //    DoSerialize(new NestedClass(100));
        //    AssertAndGoToStart(0, 0, 0x64, 0xC0, 0x80, SubTypeConverter.OUTPUT_BYTE, 0xC0, SubTypeConverter.OUTPUT_BYTE, 0, 0x64, 9);
        //    Assert.AreEqual(new NestedClass(100), DoDeserialize<NestedClass>());

        //    // Without version
        //    ResetPosition();
        //    DoSerialize(new NestedClass(100));
        //    AssertAndGoToStart(0x64, 0xE0, SubTypeConverter.OUTPUT_BYTE, 0xC0, SubTypeConverter.OUTPUT_BYTE, 0x64, 9);
        //    Assert.AreEqual(new NestedClass(100), DoDeserialize<NestedClass>());
        //}

        //[TestMethod]
        //public void Object_NoBaseMembers_FirstMemberUsesHeader()
        //{
        //    Setup<BaseWithHeader>(ABSaveSettings.ForSpeed);

        //    var obj = new SubWithHeaderHolder(new SubWithHeader());

        //    // With version
        //    Serializer.SerializeItem(obj, CurrentMapItem);
        //    AssertAndGoToStart(0xC0, 0xC0, 0x80, SubTypeConverter.OUTPUT_BYTE);
        //    Assert.AreEqual((BaseWithHeader)obj, (BaseWithHeader)Deserializer.DeserializeItem(CurrentMapItem));

        //    // Without version
        //    ResetPosition();
        //    Serializer.SerializeItem(obj, CurrentMapItem);
        //    AssertAndGoToStart(0xF8, SubTypeConverter.OUTPUT_BYTE);
        //    Assert.AreEqual((BaseWithHeader)obj, (BaseWithHeader)Deserializer.DeserializeItem(CurrentMapItem));
        //}

        //[TestMethod]
        //public void Object_WithBaseMembers_Single()
        //{
        //    Setup<MemberInheritingDoubleClass>(ABSaveSettings.ForSpeed,
        //        new() { { typeof(MemberInheritingDoubleClass), 0 }, { typeof(SingleMemberClass), 0 } });

        //    DoSerialize(new MemberInheritingDoubleClass(5, 255, 15));
        //    AssertAndGoToStart(0, 0, 0, 5, 15);
        //    Assert.AreEqual(new MemberInheritingDoubleClass(5, 0, 15), DoDeserialize<MemberInheritingDoubleClass>());
        //}

        //[TestMethod]
        //public void Object_WithBaseMembers_Double()
        //{
        //    Setup<MemberInheritingDoubleClass>(ABSaveSettings.ForSpeed,
        //        new() { { typeof(MemberInheritingDoubleClass), 1 }, { typeof(MemberInheritingSingleClass), 1 } });

        //    DoSerialize(new MemberInheritingDoubleClass(5, 255, 15));
        //    AssertAndGoToStart(1, 1, 0, 0, 5, 255, 15);
        //    Assert.AreEqual(new MemberInheritingDoubleClass(5, 255, 15), DoDeserialize<MemberInheritingDoubleClass>());
        //}

        //[TestMethod]
        //public void Object_WithBaseMembers_FirstMemberUsesHeader()
        //{
        //    Setup<SubWithHeaderHolderSub>(ABSaveSettings.ForSpeed);

        //    var obj = new SubWithHeaderHolderSub(10);

        //    // With version
        //    Serializer.SerializeItem(obj, CurrentMapItem);
        //    AssertAndGoToStart(0xC0, 0, 0xC0, 0x80, SubTypeConverter.OUTPUT_BYTE, 0, 10);
        //    Assert.AreEqual(obj, (SubWithHeaderHolderSub)Deserializer.DeserializeItem(CurrentMapItem));

        //    // Without version
        //    ResetPosition();
        //    Serializer.SerializeItem(obj, CurrentMapItem);
        //    AssertAndGoToStart(0xF8, SubTypeConverter.OUTPUT_BYTE, 10);
        //    Assert.AreEqual(obj, (SubWithHeaderHolderSub)Deserializer.DeserializeItem(CurrentMapItem));
        //}

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        public void NeitherUseHeader(int version)
        {
            RunTest(new NoHeaderNoHeader(), version, 0xF0, 0xC0,
                new byte[] { 0xC0, SubTypeConverter.OUTPUT_BYTE, 0xC0, SubTypeConverter.OUTPUT_BYTE },
                new byte[] { SubTypeConverter.OUTPUT_BYTE, 0xC0, SubTypeConverter.OUTPUT_BYTE });
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        public void FirstUsesHeader(int version)
        {
            RunTest(new HeaderNoHeader(), version, 0xF8, 0xE0,
                new byte[] { 0xC0, 0x80, SubTypeConverter.OUTPUT_BYTE, 0xC0, SubTypeConverter.OUTPUT_BYTE },
                new byte[] { SubTypeConverter.OUTPUT_BYTE, 0xC0, SubTypeConverter.OUTPUT_BYTE });
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        public void SecondUsesHeader(int version)
        {
            RunTest(new NoHeaderHeader(), version, 0xF0, 0xC0,
                new byte[] { 0xC0, SubTypeConverter.OUTPUT_BYTE, 0xC0, 0x80, SubTypeConverter.OUTPUT_BYTE },
                new byte[] { SubTypeConverter.OUTPUT_BYTE, 0xE0, SubTypeConverter.OUTPUT_BYTE });
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        public void BothUseHeader(int version)
        {
            RunTest(new HeaderHeader(), version, 0xF8, 0xE0,
                new byte[] { 0xC0, 0x80, SubTypeConverter.OUTPUT_BYTE, 0xE0, SubTypeConverter.OUTPUT_BYTE },
                new byte[] { SubTypeConverter.OUTPUT_BYTE, 0xE0, SubTypeConverter.OUTPUT_BYTE });
        }

        void RunTest<T>(T instance, int version, byte noBaseExpectedFirstByte, byte baseExpectedFirstByte, byte[] versionExpected, byte[] nonVersionExpected)
            where T : BaseWithoutHeader
        {
            instance.Init(version);

            Setup<T>(ABSaveSettings.ForSpeed, new() { { typeof(T), (uint)version } });

            // With version
            byte[] baseExpectedWithVersion = version switch
            {
                0 => new byte[] { 0xC0, 0, 0xC0, 0x80, SubTypeConverter.OUTPUT_BYTE },
                1 => new byte[] { 0xC1 },
                2 => new byte[] { 0xC2, 0, 0xC0, SubTypeConverter.OUTPUT_BYTE },
                _ => throw new Exception()
            };

            Serializer.SerializeItem(instance, CurrentMapItem);
            AssertAndGoToStart(baseExpectedWithVersion.Concat(versionExpected).ToArray());
            ReflectiveAssert(instance, (T)Deserializer.DeserializeItem(CurrentMapItem));

            ResetPosition();

            // Without version
            byte[] baseExpectedWithoutVersion = version switch
            {
                0 => new byte[] { 0xF8, SubTypeConverter.OUTPUT_BYTE, baseExpectedFirstByte },
                1 => new byte[] { noBaseExpectedFirstByte },
                2 => new byte[] { 0xF0, SubTypeConverter.OUTPUT_BYTE, baseExpectedFirstByte },
                _ => throw new Exception()
            };

            Serializer.SerializeItem(instance, CurrentMapItem);
            AssertAndGoToStart(baseExpectedWithoutVersion.Concat(nonVersionExpected).ToArray());
            ReflectiveAssert(instance, (T)Deserializer.DeserializeItem(CurrentMapItem));
        }
    }
}