using ABCo.ABSave.Configuration;
using ABCo.ABSave.Converters;
using ABCo.ABSave.Serialization;
using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ABCo.ABSave.UnitTests.Converters
{
    [TestClass]
    public class OtherTests : ConverterTestBase
    {
        Action<Type> _typeSerialize;
        Func<Type> _typeDeserialize;

        [TestMethod]
        public void Guid()
        {
            Setup<Guid>(ABSaveSettings.ForSpeed);
            var guid = new Guid("01234567-89ab-0123-4567-89abcdef0123");

            DoSerialize(guid);
            AssertAndGoToStart(Concat(0, guid.ToByteArray()));

            Assert.AreEqual(guid, DoDeserialize<Guid>());
        }

        [TestMethod]
        public void DateTime()
        {
            Setup<DateTime>(ABSaveSettings.ForSpeed);
            var dateTime = new DateTime(1989, 6, 3, 7, 3, 8);

            DoSerialize(dateTime);
            AssertAndGoToStart(Concat(0, BitConverter.GetBytes(dateTime.Ticks)));

            Assert.AreEqual(dateTime, DoDeserialize<DateTime>());
        }

        [TestMethod]
        public void TimeSpan()
        {
            Setup<TimeSpan>(ABSaveSettings.ForSpeed);
            var timeSpan = new TimeSpan(19, 7, 3, 8);

            DoSerialize(timeSpan);
            AssertAndGoToStart(Concat(0, BitConverter.GetBytes(timeSpan.Ticks)));

            Assert.AreEqual(timeSpan, DoDeserialize<TimeSpan>());
        }

        [TestMethod]
        public void KeyValue()
        {
            Setup<KeyValuePair<byte, bool>>(ABSaveSettings.ForSpeed);
            var obj = new KeyValuePair<byte, bool>(234, true);

            DoSerialize(obj);
            AssertAndGoToStart(0, 0, 234, 0, 1);

            Assert.AreEqual(obj, DoDeserialize<KeyValuePair<byte, bool>>());
        }

        //[TestMethod]
        //public void DictionaryEntry()
        //{
        //    Setup<DictionaryEntry>(ABSaveSettings.ForSpeed);
        //    var obj = new DictionaryEntry(new SubNoConverter(5), new SubWithoutHeader());

        //    DoSerialize(obj);
        //    AssertAndGoToStart(0, 162, 0, 5, 161, SubTypeConverter.OUTPUT_BYTE);

        //    Assert.AreEqual(obj, DoDeserialize<DictionaryEntry>());
        //}

        [TestMethod]
        public void Version()
        {
            Setup<Version>(ABSaveSettings.ForSpeed);

            Version[] versions = new Version[]
            {
                new Version(1258215, 567, 0, 0),
                new Version(1258215, 0, 0, 0),
                new Version(1, 1258215, 0, 0),
                new Version(1, 0, 1258215, 0),
                new Version(1, 0, 0, 1258215)
            };
            
            for (int i = 0; i < versions.Length; i++)
                DoSerialize(versions[i]);

            GoToStart();

            for (int i = 0; i < versions.Length; i++)
                Assert.AreEqual(versions[i], DoDeserialize<Version>());
        }

        [TestMethod]
        public void Assembly_NoCulture_PublicKeyToken()
        {
            Setup<Assembly>(ABSaveSettings.ForSpeed);
            var assembly = typeof(OtherTests).Assembly;

            // Non-saved
            {
                DoSerialize(assembly);
                AssertAndGoToStart(GetByteArr(
                    new object[] { typeof(OtherTests).Assembly.GetName().Name, typeof(OtherTests).Assembly.GetName().GetPublicKeyToken() }, 
                    0, 96, 0, 21, (short)GenType.String, (short)GenType.ByteArr));
                Assert.AreEqual(assembly, DoDeserialize<Assembly>());
            }

            // Saved
            ResetPosition();
            {
                DoSerialize(assembly);
                AssertAndGoToStart(128);
                Assert.AreEqual(assembly, DoDeserialize<Assembly>());
            }
        }

        [TestMethod]
        public void Type()
        {
            Setup<Type>(ABSaveSettings.ForSpeed);

            _typeSerialize = t => DoSerialize(t);
            _typeDeserialize = () => DoDeserialize<Type>();

            Action<ABSaveSerializer> convAsm = s =>
            {
                var header = new BitTarget(s, 8);
                AssemblyConverter.SerializeAssembly(typeof(BaseIndex).Assembly, ref header);
            };

            // Non-generic
            {
                TestType(typeof(BaseIndex), GetByteArr(
                    new object[] { convAsm, typeof(BaseIndex).FullName },
                    0, 0, (short)GenType.Action, 43, (short)GenType.String));
            }

            // Generic
            ResetState();
            {
                var genericType = typeof(GenericType<,,>);
                var filledType = typeof(GenericType<,,>).MakeGenericType(typeof(SubWithHeader), genericType.GetGenericArguments()[1], typeof(SubWithoutHeader));

                TestType(filledType, GetByteArr(
                    new object[] { convAsm, typeof(GenericType<,,>).FullName },
                    0, 0, (short)GenType.Action, 47, (short)GenType.String, 192, 97));
            }
        }

        [TestMethod]
        public unsafe void Type_Closed()
        {
            Setup<Type>(ABSaveSettings.ForSpeed);
            SaveCurrentAssembly();

            _typeSerialize = t => Serializer.WriteClosedType(t);
            _typeDeserialize = () => Deserializer.ReadClosedType(typeof(object));

            // Non-generic
            {
                var type = typeof(BaseIndex);
                TestType(type, GetByteArr(
                    new object[] { type.FullName }, 
                    0, 128, 43, (short)GenType.String));
            }

            // Generic
            ResetState();
            SaveCurrentAssembly();
            {
                var type = typeof(GenericType<SubWithHeader, SubWithoutHeader, SubNoConverter>);
                TestType(type, GetByteArr(
                    new object[] { typeof(GenericType<,,>).FullName }, 
                    0, 128, 47, (short)GenType.String, 128, 129, 130));
            }
        }

        void TestType(Type type, params byte[] expected)
        {
            // Saved
            ResetPosition();
            {
                _typeSerialize(type);
                AssertAndGoToStart(expected);
                Assert.AreEqual(type, _typeDeserialize());
            }

            // Non-saved
            ResetPosition();
            {
                _typeSerialize(type);
                AssertAndGoToStart((byte)(127 + Serializer.SavedTypes.Count));
                Assert.AreEqual(type, _typeDeserialize());
            }
        }

        void SaveCurrentAssembly()
        {
            // Save the current assembly so we don't have to test for it.
            Serializer.SavedAssemblies.Add(typeof(BaseIndex).Assembly, Serializer.SavedAssemblies.Count);
            Deserializer.SavedAssemblies.Add(typeof(BaseIndex).Assembly);
        }

        //[TestMethod]
        //public void Type()
        //{
        //    Setup<Type>(ABSaveSettings.PrioritizePerformance, TypeConverter.Instance);
        //    var assembly = typeof(ABSaveSerializer).Assembly;

        //    // Non-saved
        //    {
        //        DoSerialize(assembly);
        //        AssertAndGoToStart(GetExpected(false));
        //        Assert.AreEqual(assembly, DoDeserialize<Assembly>());
        //    }

        //    // Saved
        //    ResetOutput();
        //    {
        //        DoSerialize(assembly);
        //        AssertAndGoToStart(GetExpected(true));
        //        Assert.AreEqual(assembly, DoDeserialize<Assembly>());
        //    }

        //    byte[] GetExpected(bool hasKey)
        //    {
        //        var expectedOut = new MemoryStream();
        //        var expected = new ABSaveSerializer(expectedOut, CurrentMap);

        //        if (hasKey)
        //            expected.WriteByte(132);
        //        expected.WriteByte(213); // 17 for "ABSoftware.ABSave"
        //        expected.WriteString(assembly.GetName().Name);
        //        VersionConverter.Instance.Serialize(assembly.GetName().Version, typeof(Version), null, expected);
        //        expected.WriteByteArray(assembly.GetName().GetPublicKeyToken());

        //        return expectedOut.ToArray();
        //    }
        //}
    }
}
