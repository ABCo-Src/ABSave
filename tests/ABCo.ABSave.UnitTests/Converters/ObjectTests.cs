﻿using ABCo.ABSave.Configuration;
using ABCo.ABSave.Exceptions;
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
            BaseWithoutHeaderMember,
        }

        [TestMethod]
        [DataRow(BaseClass.BaseWithHeaderMember)]
        [DataRow(BaseClass.None)]
        [DataRow(BaseClass.BaseWithoutHeaderMember)]
        public void EmptyClass(BaseClass version)
        {
            RunTest(new NoMembersSub(), (int)version, 0x80, null,
                Array.Empty<byte>(),
                Array.Empty<byte>());
        }

        [TestMethod]
        [DataRow(BaseClass.BaseWithHeaderMember)]
        [DataRow(BaseClass.None)]
        [DataRow(BaseClass.BaseWithoutHeaderMember)]
        public void NeitherUseHeader(BaseClass version)
        {
            RunTest(new NoHeaderNoHeader(), (int)version, 0xC0, 0x80,
                new byte[] { 0x80, SubTypeConverter.OUTPUT_BYTE, 0x80, SubTypeConverter.OUTPUT_BYTE },
                new byte[] { SubTypeConverter.OUTPUT_BYTE, 0x80, SubTypeConverter.OUTPUT_BYTE });
        }

        [TestMethod]
        [DataRow(BaseClass.BaseWithHeaderMember)]
        [DataRow(BaseClass.None)]
        [DataRow(BaseClass.BaseWithoutHeaderMember)]
        public void FirstUsesHeader(BaseClass version)
        {
            RunTest(new HeaderNoHeader(), (int)version, 0xE0, 0xC0,
                new byte[] { 0x80, 0x80, SubTypeConverter.OUTPUT_BYTE, 0x80, SubTypeConverter.OUTPUT_BYTE },
                new byte[] { SubTypeConverter.OUTPUT_BYTE, 0x80, SubTypeConverter.OUTPUT_BYTE });
        }

        [TestMethod]
        [DataRow(BaseClass.BaseWithHeaderMember)]
        [DataRow(BaseClass.None)]
        [DataRow(BaseClass.BaseWithoutHeaderMember)]
        public void SecondUsesHeader(BaseClass version)
        {
            RunTest(new NoHeaderHeader(), (int)version, 0xC0, 0x80,
                new byte[] { 0x80, SubTypeConverter.OUTPUT_BYTE, 0x80, 0x80, SubTypeConverter.OUTPUT_BYTE },
                new byte[] { SubTypeConverter.OUTPUT_BYTE, 0xC0, SubTypeConverter.OUTPUT_BYTE });
        }

        [TestMethod]
        [DataRow(BaseClass.BaseWithHeaderMember)]
        [DataRow(BaseClass.None)]
        [DataRow(BaseClass.BaseWithoutHeaderMember)]
        public void BothUseHeader(BaseClass version)
        {
            RunTest(new HeaderHeader(), (int)version, 0xE0, 0xC0,
                new byte[] { 0x80, 0x80, SubTypeConverter.OUTPUT_BYTE, 0xC0, SubTypeConverter.OUTPUT_BYTE },
                new byte[] { SubTypeConverter.OUTPUT_BYTE, 0xC0, SubTypeConverter.OUTPUT_BYTE });
        }

        [TestMethod]
        public void Invalid_IncorrectBase() => Assert.ThrowsException<InvalidSaveBaseMembersException>(() => Setup<CompletelyInvalidBase>(ABSaveSettings.ForSpeed));

        [TestMethod]
        public void Invalid_UnserializableBase() => Assert.ThrowsException<InvalidSaveBaseMembersException>(() => Setup<UnserializableBase>(ABSaveSettings.ForSpeed));

        [TestMethod]
        public void Initialize_NoPublicConstructor_ShouldThrowException() => 
	        Assert.ThrowsException<UnsupportedTypeException>(() => 
		        Setup<PrivateConstructor>(ABSaveSettings.ForSpeed));

        [TestMethod]
        public void Initialize_NoParameterlessConstructor_ShouldThrowException() =>
	        Assert.ThrowsException<UnsupportedTypeException>(() =>
		        Setup<NoParameterlessConstructor>(ABSaveSettings.ForSpeed));

        [TestMethod]
        public void SerializeAndDeserialize_AbstractClass_ShouldThrowException()
        {
            Setup<AbstractClass>(ABSaveSettings.ForSpeed);

            Assert.ThrowsException<UnsupportedTypeException>(() => DoSerialize(new AbstractClassSub()));

            Serializer.Flush();
            GoToStart();

            Assert.ThrowsException<UnsupportedTypeException>(DoDeserialize<AbstractClass>);
        }

        void RunTest<T>(T instance, int version, byte? noBaseExpectedFirstByte, byte? baseExpectedFirstByte, byte[] versionExpected, byte[] nonVersionExpected)
            where T : BaseWithoutHeader
        {
            instance.Init(version);

            Setup<T>(ABSaveSettings.ForSpeed, new Dictionary<Type, uint>() { { typeof(T), (uint)version } });

            // With version
            byte[] baseExpectedWithVersion = version switch
            {
                0 => new byte[] { 0x80, 0x00, 0x80, 0x80, SubTypeConverter.OUTPUT_BYTE },
                1 => new byte[] { 0x81 },
                2 => new byte[] { 0x82, 0x00, 0x80, SubTypeConverter.OUTPUT_BYTE },
                //3 => new byte[] { 0xC3, 0 },
                _ => throw new Exception()
            };

            Serializer.WriteItem(instance, CurrentMapItem);
            AssertAndGoToStart(baseExpectedWithVersion.Concat(versionExpected).ToArray());
            ReflectiveAssert(instance, (T)Deserializer.ReadItem(CurrentMapItem));

            ClearStream();

            // Without version
            byte[] baseExpectedWithoutVersion = version switch
            {
                0 => baseExpectedFirstByte == null ? new byte[] { 0xE0, SubTypeConverter.OUTPUT_BYTE } : new byte[] { 0xE0, SubTypeConverter.OUTPUT_BYTE, baseExpectedFirstByte.Value },
                1 => noBaseExpectedFirstByte == null ? Array.Empty<byte>() : new byte[] { noBaseExpectedFirstByte.Value },
                2 => baseExpectedFirstByte == null ? new byte[] { 0xC0, SubTypeConverter.OUTPUT_BYTE } : new byte[] { 0xC0, SubTypeConverter.OUTPUT_BYTE, baseExpectedFirstByte.Value },
                //3 => noBaseExpectedFirstByte == null ? Array.Empty<byte>() : new byte[] { noBaseExpectedFirstByte.Value },
                _ => throw new Exception()
            };

            Serializer.WriteItem(instance, CurrentMapItem);
            AssertAndGoToStart(baseExpectedWithoutVersion.Concat(nonVersionExpected).ToArray());
            ReflectiveAssert(instance, (T)Deserializer.ReadItem(CurrentMapItem));
        }
    }

    [SaveMembers]
    public class PrivateConstructor
    {
        private PrivateConstructor()
        {
				
        }

        [Save(0)]
        public int A { get; set; }
    }


    [SaveMembers]
    public class NoParameterlessConstructor
    {
	    public NoParameterlessConstructor(int a)
	    {
		    A = a;
	    }

	    [Save(0)]
	    public int A { get; set; }
    }

    [SaveMembers]
    public abstract class AbstractClass
    {

    }

    [SaveMembers]
    public class AbstractClassSub : AbstractClass
    {

    }

    [SaveBaseMembers(typeof(string))]
    [SaveMembers]
    class CompletelyInvalidBase { }

    class Unserializable { }

    [SaveBaseMembers(typeof(Unserializable))]
    [SaveMembers]
    class UnserializableBase : Unserializable { }

    [SaveMembers]
    class EmptyBase { }

    [SaveMembers]
    class BaseWithHeader : EmptyBase
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
    [SaveBaseMembers(typeof(EmptyBase), FromVer = 3)]
    class NoMembersSub : BaseWithoutHeader
    {
        public NoMembersSub() { }
        protected override void SubInit() { }
    }

    [SaveMembers]
    [SaveBaseMembers(typeof(BaseWithHeader), ToVer = 1)]
    [SaveBaseMembers(typeof(BaseWithoutHeader), FromVer = 2)]
    [SaveBaseMembers(typeof(EmptyBase), FromVer = 3)]
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
    [SaveBaseMembers(typeof(EmptyBase), FromVer = 3)]
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
    [SaveBaseMembers(typeof(EmptyBase), FromVer = 3)]
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
    [SaveBaseMembers(typeof(EmptyBase), FromVer = 3)]
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