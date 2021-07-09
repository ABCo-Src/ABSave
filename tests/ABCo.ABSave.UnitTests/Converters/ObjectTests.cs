using ABCo.ABSave.Configuration;
using ABCo.ABSave.Mapping.Description.Attributes;
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
            BaseWithHeaderMember,
            None,
            BaseWithoutHeaderMember
        }

        [TestMethod]
        [DataRow(BaseClass.BaseWithHeaderMember)]
        [DataRow(BaseClass.None)]
        [DataRow(BaseClass.BaseWithoutHeaderMember)]
        public void NeitherUseHeader(BaseClass version)
        {
            RunTest(new NoHeaderNoHeader(), (int)version, 0xF0, 0xC0,
                new byte[] { 0xC0, SubTypeConverter.OUTPUT_BYTE, 0xC0, SubTypeConverter.OUTPUT_BYTE },
                new byte[] { SubTypeConverter.OUTPUT_BYTE, 0xC0, SubTypeConverter.OUTPUT_BYTE });
        }

        [TestMethod]
        [DataRow(BaseClass.BaseWithHeaderMember)]
        [DataRow(BaseClass.None)]
        [DataRow(BaseClass.BaseWithoutHeaderMember)]
        public void FirstUsesHeader(BaseClass version)
        {
            RunTest(new HeaderNoHeader(), (int)version, 0xF8, 0xE0,
                new byte[] { 0xC0, 0x80, SubTypeConverter.OUTPUT_BYTE, 0xC0, SubTypeConverter.OUTPUT_BYTE },
                new byte[] { SubTypeConverter.OUTPUT_BYTE, 0xC0, SubTypeConverter.OUTPUT_BYTE });
        }

        [TestMethod]
        [DataRow(BaseClass.BaseWithHeaderMember)]
        [DataRow(BaseClass.None)]
        [DataRow(BaseClass.BaseWithoutHeaderMember)]
        public void SecondUsesHeader(BaseClass version)
        {
            RunTest(new NoHeaderHeader(), (int)version, 0xF0, 0xC0,
                new byte[] { 0xC0, SubTypeConverter.OUTPUT_BYTE, 0xC0, 0x80, SubTypeConverter.OUTPUT_BYTE },
                new byte[] { SubTypeConverter.OUTPUT_BYTE, 0xE0, SubTypeConverter.OUTPUT_BYTE });
        }

        [TestMethod]
        [DataRow(BaseClass.BaseWithHeaderMember)]
        [DataRow(BaseClass.None)]
        [DataRow(BaseClass.BaseWithoutHeaderMember)]
        public void BothUseHeader(BaseClass version)
        {
            RunTest(new HeaderHeader(), (int)version, 0xF8, 0xE0,
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

    [SaveMembers]
    class BaseWithHeader
    {
        [Save(0)]
        public SubWithHeader2 BaseA { get; set; }
    }

    [SaveMembers]
    abstract class BaseWithoutHeader : BaseWithHeader
    {
        [Save(0)]
        public SubWithoutHeader2 BaseB { get; set; }

        public void Init(int ver)
        {
            if (ver == 0)
                BaseA = new SubWithHeader2();
            else if (ver == 2)
                BaseB = new SubWithoutHeader2();

            SubInit();
        }

        protected abstract void SubInit();
    }

    [SaveMembers]
    [SaveBaseMembers(typeof(BaseWithHeader), ToVer = 1)]
    [SaveBaseMembers(typeof(BaseWithoutHeader), FromVer = 2)]
    class NoHeaderNoHeader : BaseWithoutHeader
    {
        [Save(0)]
        public SubWithoutHeader A { get; set; }

        [Save(1)]
        public SubWithoutHeader B { get; set; }

        public NoHeaderNoHeader() { }
        protected override void SubInit() => (A, B) = (new SubWithoutHeader(), new SubWithoutHeader());
    }

    [SaveMembers]
    [SaveBaseMembers(typeof(BaseWithHeader), ToVer = 1)]
    [SaveBaseMembers(typeof(BaseWithoutHeader), FromVer = 2)]
    class HeaderNoHeader : BaseWithoutHeader
    {
        [Save(0)]
        public SubWithHeader A { get; set; }

        [Save(1)]
        public SubWithoutHeader B { get; set; }

        public HeaderNoHeader() { }
        protected override void SubInit() => (A, B) = (new SubWithHeader(), new SubWithoutHeader());
    }

    [SaveMembers]
    [SaveBaseMembers(typeof(BaseWithHeader), ToVer = 1)]
    [SaveBaseMembers(typeof(BaseWithoutHeader), FromVer = 2)]
    class NoHeaderHeader : BaseWithoutHeader
    {
        [Save(0)]
        public SubWithoutHeader A { get; set; }

        [Save(1)]
        public SubWithHeader B { get; set; }

        public NoHeaderHeader() { }
        protected override void SubInit() => (A, B) = (new SubWithoutHeader(), new SubWithHeader());
    }

    [SaveMembers]
    [SaveBaseMembers(typeof(BaseWithHeader), ToVer = 1)]
    [SaveBaseMembers(typeof(BaseWithoutHeader), FromVer = 2)]
    class HeaderHeader : BaseWithoutHeader
    {
        [Save(0)]
        public SubWithHeader A { get; set; }

        [Save(1)]
        public SubWithHeader B { get; set; }

        public HeaderHeader() { }
        protected override void SubInit() => (A, B) = (new SubWithHeader(), new SubWithHeader());
    }
}