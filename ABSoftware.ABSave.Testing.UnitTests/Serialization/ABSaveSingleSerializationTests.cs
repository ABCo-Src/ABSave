using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Serialization;
using ABSoftware.ABSave.Serialization.Writer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave.Testing.UnitTests.Serialization
{
    [TestClass]
    public class ABSaveSingleSerializationTests
    {
        [TestMethod]
        public void SerializeVersion()
        {
            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            VersionTypeConverter.Instance.Serialize(new Version(1258215, 567, 0, 0), new Helpers.TypeInformation(), actual);
            VersionTypeConverter.Instance.Serialize(new Version(1258215, 0, 0, 0), new Helpers.TypeInformation(), actual);
            VersionTypeConverter.Instance.Serialize(new Version(1, 1258215, 0, 0), new Helpers.TypeInformation(), actual);
            VersionTypeConverter.Instance.Serialize(new Version(1, 0, 1258215, 0), new Helpers.TypeInformation(), actual);
            VersionTypeConverter.Instance.Serialize(new Version(1, 0, 0, 1258215), new Helpers.TypeInformation(), actual);

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteByte(12);
            expected.WriteInt32(1258215);
            expected.WriteInt32(567);
            expected.WriteByte(8);
            expected.WriteInt32(1258215);
            expected.WriteByte(4);
            expected.WriteInt32(1258215);
            expected.WriteByte(2);
            expected.WriteInt32(1258215);
            expected.WriteByte(1);
            expected.WriteInt32(1258215);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void SerializeAssembly_NoCulture_PublicKeyToken(bool writeKey)
        {
            var assembly = typeof(ABSaveItemSerializer).Assembly;
            var actual = new ABSaveMemoryWriter(new ABSaveSettings().SetCacheTypesAndAssemblies(writeKey));
            AssemblyTypeConverter.Instance.Serialize(assembly, new Helpers.TypeInformation(), actual);

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            if (writeKey)
                expected.WriteByte(0);
            expected.WriteByte(3);
            expected.WriteText(assembly.GetName().Name);
            VersionTypeConverter.Instance.Serialize(assembly.GetName().Version, new Helpers.TypeInformation(), expected);
            expected.WriteByteArray(assembly.GetName().GetPublicKeyToken(), false);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void SerializeType(bool writeKey)
        {
            var type = typeof(ABSaveItemSerializer);
            var actual = new ABSaveMemoryWriter(new ABSaveSettings().SetCacheTypesAndAssemblies(writeKey));
            TypeTypeConverter.Instance.Serialize(type, new Helpers.TypeInformation(), actual);

            var expected = new ABSaveMemoryWriter(new ABSaveSettings().SetCacheTypesAndAssemblies(writeKey));
            if (writeKey)
                expected.WriteByte(0);
            AssemblyTypeConverter.Instance.Serialize(type.Assembly, new Helpers.TypeInformation(), expected);
            expected.WriteText(type.FullName);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        public void SerializeBoolean()
        {
            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            BooleanTypeConverter.Instance.Serialize(false, new Helpers.TypeInformation(), actual);
            BooleanTypeConverter.Instance.Serialize(true, new Helpers.TypeInformation(), actual);

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteByte(0);
            expected.WriteByte(1);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        public void SerializeNumber()
        {
            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            NumberAndEnumTypeConverter.Instance.Serialize((byte)72, new Helpers.TypeInformation(typeof(byte), TypeCode.Byte), actual);
            NumberAndEnumTypeConverter.Instance.Serialize((sbyte)72, new Helpers.TypeInformation(typeof(sbyte), TypeCode.SByte), actual);
            NumberAndEnumTypeConverter.Instance.Serialize((short)72, new Helpers.TypeInformation(typeof(short), TypeCode.Int16), actual);
            NumberAndEnumTypeConverter.Instance.Serialize((ushort)72, new Helpers.TypeInformation(typeof(ushort), TypeCode.UInt16), actual);
            NumberAndEnumTypeConverter.Instance.Serialize(72, new Helpers.TypeInformation(typeof(int), TypeCode.Int32), actual);
            NumberAndEnumTypeConverter.Instance.Serialize((uint)72, new Helpers.TypeInformation(typeof(uint), TypeCode.UInt32), actual);
            NumberAndEnumTypeConverter.Instance.Serialize((long)72, new Helpers.TypeInformation(typeof(long), TypeCode.Int64), actual);
            NumberAndEnumTypeConverter.Instance.Serialize((ulong)72, new Helpers.TypeInformation(typeof(ulong), TypeCode.UInt64), actual);
            NumberAndEnumTypeConverter.Instance.Serialize((float)72, new Helpers.TypeInformation(typeof(float), TypeCode.Single), actual);
            NumberAndEnumTypeConverter.Instance.Serialize((double)72, new Helpers.TypeInformation(typeof(double), TypeCode.Double), actual);
            NumberAndEnumTypeConverter.Instance.Serialize((decimal)72, new Helpers.TypeInformation(typeof(decimal), TypeCode.Decimal), actual);
            NumberAndEnumTypeConverter.Instance.Serialize('A', new Helpers.TypeInformation(typeof(char), TypeCode.Char), actual);

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteByte(72);
            expected.WriteByte(72);
            expected.WriteInt16(72);
            expected.WriteInt16(72);
            expected.WriteInt32(72);
            expected.WriteInt32(72);
            expected.WriteInt64(72);
            expected.WriteInt64(72);
            expected.WriteSingle(72);
            expected.WriteDouble(72);
            expected.WriteDecimal(72);
            expected.WriteInt16(65);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        public void SerializeNumber_Enum()
        {
            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            NumberAndEnumTypeConverter.Instance.Serialize(TestEnum.Item2, new Helpers.TypeInformation(typeof(TestEnum), TypeCode.Int32), actual);

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteInt32(2);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        public void SerializeString()
        {
            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            StringTypeConverter.Instance.Serialize("abc", new Helpers.TypeInformation(), actual);

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteText("abc");

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        public void SerializeStringBuilder()
        {
            var str = new StringBuilder(3);
            str.Append("abc");

            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            StringBuilderTypeConverter.Instance.Serialize(str, new Helpers.TypeInformation(), actual);

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteText("abc");

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        public void SerializeGUID()
        {
            var guid = new Guid("01234567-89ab-0123-4567-89abcdef0123");

            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            GuidTypeConverter.Instance.Serialize(guid, new Helpers.TypeInformation(), actual);

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteByteArray(guid.ToByteArray(), false);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        public void SerializeDateTime()
        {
            var dateTime = new DateTime(116);

            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            DateTimeTypeConverter.Instance.Serialize(dateTime, new Helpers.TypeInformation(), actual);

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteInt64((ulong)dateTime.Ticks);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }

        [TestMethod]
        public void SerializeTimeSpan()
        {
            var timeSpan = new TimeSpan(116);

            var actual = new ABSaveMemoryWriter(new ABSaveSettings());
            TimeSpanTypeConverter.Instance.Serialize(timeSpan, new Helpers.TypeInformation(), actual);

            var expected = new ABSaveMemoryWriter(new ABSaveSettings());
            expected.WriteInt64((ulong)timeSpan.Ticks);

            CollectionAssert.AreEqual(expected.ToBytes(), actual.ToBytes());
        }
    }

    enum TestEnum
    {
        Item1 = 1,
        Item2 = 2,
        Item3 = 4
    }
}
