using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Testing.UnitTests.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave.Testing.UnitTests.Deserialization
{
    [TestClass]
    public class SingleDeserializationTests
    {
        [TestMethod]
        public void DeserializeVersion()
        {
            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());

            var expected = new Version[] {
                new Version(1258215, 567, 0, 0),
                new Version(1258215, 0, 0, 0),
                new Version(1, 1258215, 0, 0),
                new Version(1, 0, 1258215, 0),
                new Version(1, 0, 0, 1258215)
            };

            VersionTypeConverter.Instance.Serialize(expected[0], typeof(Version), writer);
            VersionTypeConverter.Instance.Serialize(expected[1], typeof(Version), writer);
            VersionTypeConverter.Instance.Serialize(expected[2], typeof(Version), writer);
            VersionTypeConverter.Instance.Serialize(expected[3], typeof(Version), writer);
            VersionTypeConverter.Instance.Serialize(expected[4], typeof(Version), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());

            var actual = new Version[5];
            for (int i = 0; i < 5; i++)
                actual[i] = (Version)VersionTypeConverter.Instance.Deserialize(typeof(Version), reader);

            CollectionAssert.AreEqual(actual, expected);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void DeserializeAssembly_NoCulture_PublicKeyToken(bool writeKey)
        {
            var expected = typeof(ABSaveItemConverter).Assembly;
            var settings = new ABSaveSettings().SetCacheTypesAndAssemblies(writeKey);

            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, settings);
            AssemblyTypeConverter.Instance.Serialize(expected, typeof(Assembly), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, settings);
            var actual = (Assembly)AssemblyTypeConverter.Instance.Deserialize(typeof(Assembly), reader);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void DeserializeType(bool writeKey)
        {
            var expected = typeof(ABSaveItemConverter);
            var settings = new ABSaveSettings().SetCacheTypesAndAssemblies(writeKey);

            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, settings);
            TypeTypeConverter.Instance.Serialize(expected, typeof(Type), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, settings);
            var actual = (Type)TypeTypeConverter.Instance.Deserialize(typeof(Type), reader);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DeserializeBoolean()
        {
            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());

            BooleanTypeConverter.Instance.Serialize(false, typeof(bool), writer);
            BooleanTypeConverter.Instance.Serialize(true, typeof(bool), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            Assert.IsFalse((bool)BooleanTypeConverter.Instance.Deserialize(typeof(bool), reader));
            Assert.IsTrue((bool)BooleanTypeConverter.Instance.Deserialize(typeof(bool), reader));
        }

        [TestMethod]
        public void DeserializeNumber()
        {
            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());

            NumberTypeConverter.Instance.Serialize((byte)72, typeof(byte), writer);
            NumberTypeConverter.Instance.Serialize((sbyte)72, typeof(sbyte), writer);
            NumberTypeConverter.Instance.Serialize((short)72, typeof(short), writer);
            NumberTypeConverter.Instance.Serialize((ushort)72, typeof(ushort), writer);
            NumberTypeConverter.Instance.Serialize(72, typeof(int), writer);
            NumberTypeConverter.Instance.Serialize((uint)72, typeof(uint), writer);
            NumberTypeConverter.Instance.Serialize((long)72, typeof(long), writer);
            NumberTypeConverter.Instance.Serialize((ulong)72, typeof(ulong), writer);
            NumberTypeConverter.Instance.Serialize((float)72, typeof(float), writer);
            NumberTypeConverter.Instance.Serialize((double)72, typeof(double), writer);
            NumberTypeConverter.Instance.Serialize((decimal)72, typeof(decimal), writer);
            NumberTypeConverter.Instance.Serialize('A', typeof(char), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());

            Assert.AreEqual((byte)72, NumberTypeConverter.Instance.Deserialize(typeof(byte), reader));
            Assert.AreEqual((sbyte)72, NumberTypeConverter.Instance.Deserialize(typeof(sbyte), reader));
            Assert.AreEqual((short)72, NumberTypeConverter.Instance.Deserialize(typeof(short), reader));
            Assert.AreEqual((ushort)72, NumberTypeConverter.Instance.Deserialize(typeof(ushort), reader));
            Assert.AreEqual(72, NumberTypeConverter.Instance.Deserialize(typeof(int), reader));
            Assert.AreEqual((uint)72, NumberTypeConverter.Instance.Deserialize(typeof(uint), reader));
            Assert.AreEqual((long)72, NumberTypeConverter.Instance.Deserialize(typeof(long), reader));
            Assert.AreEqual((ulong)72, NumberTypeConverter.Instance.Deserialize(typeof(ulong), reader));
            Assert.AreEqual((float)72, NumberTypeConverter.Instance.Deserialize(typeof(float), reader));
            Assert.AreEqual((double)72, NumberTypeConverter.Instance.Deserialize(typeof(double), reader));
            Assert.AreEqual((decimal)72, NumberTypeConverter.Instance.Deserialize(typeof(decimal), reader));
            Assert.AreEqual('A', NumberTypeConverter.Instance.Deserialize(typeof(char), reader));
        }

        [TestMethod]
        public void DeserializeNumber_Enum()
        {
            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());

            NumberTypeConverter.Instance.Serialize(TestEnum.Item2, typeof(TestEnum), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());

            Assert.AreEqual(TestEnum.Item2, (TestEnum)NumberTypeConverter.Instance.Deserialize(typeof(TestEnum), reader));
        }

        [TestMethod]
        public void DeserializeString()
        {
            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());

            StringTypeConverter.Instance.Serialize("abc", typeof(string), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());

            Assert.AreEqual("abc", (string)StringTypeConverter.Instance.Deserialize(typeof(string), reader));
        }

        [TestMethod]
        public void DeserializeStringBuilder()
        {
            var actual = new StringBuilder();
            actual.Append("abc");

            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());

            StringBuilderTypeConverter.Instance.Serialize(actual, typeof(StringBuilder), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            var expected = (StringBuilder)StringBuilderTypeConverter.Instance.Deserialize(typeof(StringBuilder), reader);

            Assert.AreEqual(expected.ToString(), actual.ToString());
        }

        [TestMethod]
        public void DeserializeGUID()
        {
            var actual = new Guid("01234567-89ab-0123-4567-89abcdef0123");

            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());

            GuidTypeConverter.Instance.Serialize(actual, typeof(Guid), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            var expected = (Guid)GuidTypeConverter.Instance.Deserialize(typeof(Guid), reader);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DeserializeDateTime()
        {
            var actual = new DateTime(116);

            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());

            DateTimeTypeConverter.Instance.Serialize(actual, typeof(DateTime), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            var expected = (DateTime)DateTimeTypeConverter.Instance.Deserialize(typeof(DateTime), reader);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DeserializeTimeSpan()
        {
            var actual = new TimeSpan(116);

            var memoryStream = new MemoryStream();
            var writer = new ABSaveWriter(memoryStream, new ABSaveSettings());

            TimeSpanTypeConverter.Instance.Serialize(actual, typeof(TimeSpan), writer);

            memoryStream.Position = 0;
            var reader = new ABSaveReader(memoryStream, new ABSaveSettings());
            var expected = (TimeSpan)TimeSpanTypeConverter.Instance.Deserialize(typeof(TimeSpan), reader);

            Assert.AreEqual(expected, actual);
        }
    }
}