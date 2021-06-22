using ABCo.ABSave.Configuration;
using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace ABCo.ABSave.UnitTests.Core
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
                Initialize(ABSaveSettings.ForSpeed.Customize(b => b.SetUseLittleEndian(!BitConverter.IsLittleEndian)));

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
