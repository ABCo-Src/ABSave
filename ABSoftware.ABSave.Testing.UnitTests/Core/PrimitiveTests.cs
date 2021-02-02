using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Serialization;
using ABSoftware.ABSave.Testing.UnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABSoftware.ABSave.Testing.UnitTests.Core
{
    [TestClass]
    public class PrimitiveTests : TestBase
    {
        [TestMethod]
        public void Byte()
        {
            Initialize();

            Serializer.WriteByte(5);
            Serializer.WriteByte(7);
            AssertAndGoToStart(5, 7);

            Assert.AreEqual(5, Deserializer.ReadByte());
            Assert.AreEqual(7, Deserializer.ReadByte());
        }

        [DataRow(false)]
        [DataRow(true)]
        [TestMethod]
        public void Int16(bool reversed) => TestNum(d => Serializer.WriteInt16(d), () => Deserializer.ReadInt16(), (short)468, reversed);

        [DataRow(false)]
        [DataRow(true)]
        [TestMethod]
        public void Int32(bool reversed) => TestNum(d => Serializer.WriteInt32(d), () => Deserializer.ReadInt32(), 134217728, reversed);

        [DataRow(false)]
        [DataRow(true)]
        [TestMethod]
        public void Int64(bool reversed) => TestNum(d => Serializer.WriteInt64(d), () => Deserializer.ReadInt64(), long.MaxValue, reversed);

        [DataRow(false)]
        [DataRow(true)]
        [TestMethod]
        public void Single(bool reversed) => TestNum(d => Serializer.WriteSingle(d), () => Deserializer.ReadSingle(), float.MinValue, reversed);

        [DataRow(false)]
        [DataRow(true)]
        [TestMethod]
        public void Double(bool reversed) => TestNum(d => Serializer.WriteDouble(d), () => Deserializer.ReadDouble(), double.MaxValue, reversed);

        void TestNum(Action<dynamic> write, Func<dynamic> read, dynamic val, bool reversed)
        {
            if (reversed)
            {
                var builder = new ABSaveSettingsBuilder()
                {
                    UseLittleEndian = !BitConverter.IsLittleEndian
                };

                Initialize(builder.CreateSettings(ABSaveSettings.GetSpeedFocus(true)));

                write(val);
                AssertAndGoToStart(((byte[])BitConverter.GetBytes(val)).Reverse().ToArray());

                Assert.AreEqual(val, read());
            }
            else
            {
                Initialize();

                write(val);
                AssertAndGoToStart((byte[])BitConverter.GetBytes(val));

                Assert.AreEqual(val, read());
            }
        }
    }
}
