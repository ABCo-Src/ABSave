using ABSoftware.ABSave.Converters;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave.Testing.UnitTests.Serialization
{
    [TestClass]
    public class SingleSerializationTests
    {
        [TestMethod]
        public void SerializeVersion()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            VersionTypeConverter.Instance.Serialize(new Version(1258215, 567, 0, 0), typeof(Version), actual);
            VersionTypeConverter.Instance.Serialize(new Version(1258215, 0, 0, 0), typeof(Version), actual);
            VersionTypeConverter.Instance.Serialize(new Version(1, 1258215, 0, 0), typeof(Version), actual);
            VersionTypeConverter.Instance.Serialize(new Version(1, 0, 1258215, 0), typeof(Version), actual);
            VersionTypeConverter.Instance.Serialize(new Version(1, 0, 0, 1258215), typeof(Version), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
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

            TestUtilities.CompareWriters(expected, actual);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void SerializeAssembly_NoCulture_PublicKeyToken(bool writeKey)
        {
            var assembly = typeof(ABSaveItemConverter).Assembly;
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings().SetCacheTypesAndAssemblies(writeKey));
            AssemblyTypeConverter.Instance.Serialize(assembly, typeof(Assembly), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            if (writeKey)
                expected.WriteByte(0);
            expected.WriteByte(3);
            expected.WriteString(assembly.GetName().Name);
            VersionTypeConverter.Instance.Serialize(assembly.GetName().Version, typeof(Version), expected);
            expected.WriteByteArray(assembly.GetName().GetPublicKeyToken(), false);

            TestUtilities.CompareWriters(expected, actual);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void SerializeType(bool writeKey)
        {
            var type = typeof(ABSaveItemConverter);
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings().SetCacheTypesAndAssemblies(writeKey));
            TypeTypeConverter.Instance.Serialize(type, typeof(Type), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings().SetCacheTypesAndAssemblies(writeKey));
            if (writeKey)
                expected.WriteByte(0);
            AssemblyTypeConverter.Instance.Serialize(type.Assembly, typeof(Assembly), expected);
            expected.WriteString(type.FullName);

            TestUtilities.CompareWriters(expected, actual);
        }

        [TestMethod]
        public void SerializeBoolean()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            BooleanTypeConverter.Instance.Serialize(false, typeof(bool), actual);
            BooleanTypeConverter.Instance.Serialize(true, typeof(bool), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteByte(0);
            expected.WriteByte(1);

            TestUtilities.CompareWriters(expected, actual);
        }

        [TestMethod]
        public void SerializeNumber()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            NumberTypeConverter.Instance.Serialize((byte)72, typeof(byte), actual);
            NumberTypeConverter.Instance.Serialize((sbyte)72, typeof(sbyte), actual);
            NumberTypeConverter.Instance.Serialize((short)72, typeof(short), actual);
            NumberTypeConverter.Instance.Serialize((ushort)72, typeof(ushort), actual);
            NumberTypeConverter.Instance.Serialize(72, typeof(int), actual);
            NumberTypeConverter.Instance.Serialize((uint)72, typeof(uint), actual);
            NumberTypeConverter.Instance.Serialize((long)72, typeof(long), actual);
            NumberTypeConverter.Instance.Serialize((ulong)72, typeof(ulong), actual);
            NumberTypeConverter.Instance.Serialize((float)72, typeof(float), actual);
            NumberTypeConverter.Instance.Serialize((double)72, typeof(double), actual);
            NumberTypeConverter.Instance.Serialize((decimal)72, typeof(decimal), actual);
            NumberTypeConverter.Instance.Serialize('A', typeof(char), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
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

            TestUtilities.CompareWriters(expected, actual);
        }

        [TestMethod]
        public void SerializeNumber_Enum()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            NumberTypeConverter.Instance.Serialize(TestEnum.Item2, typeof(TestEnum), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteInt32(2);

            TestUtilities.CompareWriters(expected, actual);
        }

        [TestMethod]
        public void SerializeString()
        {
            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            StringTypeConverter.Instance.Serialize("abc", typeof(string), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteString("abc");

            TestUtilities.CompareWriters(expected, actual);
        }

        [TestMethod]
        public void SerializeStringBuilder()
        {
            var str = new StringBuilder(3);
            str.Append("abc");

            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            StringBuilderTypeConverter.Instance.Serialize(str, typeof(StringBuilder), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteString("abc");

            TestUtilities.CompareWriters(expected, actual);
        }

        [TestMethod]
        public void SerializeGUID()
        {
            var guid = new Guid("01234567-89ab-0123-4567-89abcdef0123");

            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            GuidTypeConverter.Instance.Serialize(guid, typeof(Guid), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteByteArray(guid.ToByteArray(), false);

            TestUtilities.CompareWriters(expected, actual);
        }

        [TestMethod]
        public void SerializeDateTime()
        {
            var dateTime = new DateTime(116);

            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            DateTimeTypeConverter.Instance.Serialize(dateTime, typeof(DateTime), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteInt64((ulong)dateTime.Ticks);

            TestUtilities.CompareWriters(expected, actual);
        }

        [TestMethod]
        public void SerializeTimeSpan()
        {
            var timeSpan = new TimeSpan(116);

            var actual = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            TimeSpanTypeConverter.Instance.Serialize(timeSpan, typeof(TimeSpan), actual);

            var expected = new ABSaveWriter(new MemoryStream(), new ABSaveSettings());
            expected.WriteInt64((ulong)timeSpan.Ticks);

            TestUtilities.CompareWriters(expected, actual);
        }
    }

    enum TestEnum
    {
        Item1 = 1,
        Item2 = 2,
        Item3 = 4
    }
}
