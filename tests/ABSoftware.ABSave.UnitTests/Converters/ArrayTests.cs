using ABCo.ABSave.Converters;
using ABCo.ABSave.Serialization;
using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ABCo.ABSave.UnitTests.Converters
{
    [TestClass]
    public class ArrayTests : ConverterTestBase
    {
        static ABSaveSettings Settings = null!;

        [TestInitialize]
        public void SetupSettings()
        {
            var builder = new ABSaveSettingsBuilder
            {
                BypassDangerousTypeChecking = true
            };
            Settings = builder.CreateSettings(ABSaveSettings.ForSpeed);
        }

        [TestMethod]
        [DataRow(false)]
        public void SZSlow(bool unknown)
        {
            var arr = new string[] { "A", "B", "C", "D", "E" };

            if (unknown)
            {
                Setup<Array>(Settings);
                DoSerialize(arr);

                Action<ABSaveSerializer> getElemType = s => s.WriteClosedType(typeof(string));

                AssertAndGoToStart(GetByteArr(new object[] { getElemType }, (short)GenType.Action, 5, 193, 65, 193, 66, 193, 67, 193, 68, 193, 69));
                CollectionAssert.AreEqual(arr, DoDeserialize<Array>());
            }
            else
            {
                Setup<string[]>(Settings);

                DoSerialize(arr);
                AssertAndGoToStart(0, 5, 192, 1, 65, 193, 66, 193, 67, 193, 68, 193, 69);
                CollectionAssert.AreEqual(arr, DoDeserialize<string[]>());
            }
        }

        //[TestMethod]
        //[DataRow(false)]
        //public void SZFast(bool unknown)
        //{
        //    var arr = new byte[] { 2, 7, 167, 43, 32 };

        //    if (unknown)
        //    {
        //        Setup<Array>(Settings);
        //        DoSerialize(arr);

        //        Action<ABSaveSerializer> getElemType = s => s.WriteClosedType(typeof(byte));

        //        AssertAndGoToStart(GetByteArr(new object[] { getElemType }, (short)GenType.Action, 5, 2, 7, 167, 43, 32));
        //        CollectionAssert.AreEqual(arr, DoDeserialize<Array>());
        //    }
        //    else
        //    {
        //        Setup<byte[]>(Settings);

        //        DoSerialize(arr);
        //        AssertAndGoToStart(0, 5, 0, 2, 7, 167, 43, 32);
        //        CollectionAssert.AreEqual(arr, DoDeserialize<byte[]>());
        //    }
        //}

        [TestMethod]
        [DataRow(false)]
        public void SNZ(bool unknown)
        {
            var arr = Array.CreateInstance(typeof(byte), new int[] { 5 }, new int[] { 2 });
            arr.SetValue((byte)2, 2);
            arr.SetValue((byte)7, 3);
            arr.SetValue((byte)167, 4);
            arr.SetValue((byte)43, 5);
            arr.SetValue((byte)32, 6);

            if (unknown)
            {
                Setup<Array>(Settings);

                DoSerialize(arr);

                Action<ABSaveSerializer> getElemType = s => s.WriteClosedType(typeof(byte));

                AssertAndGoToStart(GetByteArr(new object[] { getElemType }, (short)GenType.Action, 69, 2, 2, 7, 167, 43, 32));
                CollectionAssert.AreEqual(arr, DoDeserialize<Array>());
            }
            else
            {
                // Setup<Int32[*]>
                var method = GetType().GetMethod(nameof(Setup), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var toCall = method.MakeGenericMethod(typeof(string).Assembly.GetType("System.Byte[*]"));
                toCall.Invoke(this, new object[] { ABSaveSettings.ForSize });

                DoSerialize(arr);
                AssertAndGoToStart(0, 5, 2, 0, 2, 7, 167, 43, 32);
                CollectionAssert.AreEqual(arr, DoDeserialize<ICollection>());
            }
        }

        [TestMethod]
        [DataRow(false)]
        public void MD_ZeroLowerBounds(bool unknown)
        {
            var arr = Array.CreateInstance(typeof(byte), new int[] { 2, 3, 2 });
            arr.SetValue((byte)2, 0, 0, 0);
            arr.SetValue((byte)7, 0, 0, 1);
            arr.SetValue((byte)167, 0, 1, 0);
            arr.SetValue((byte)43, 0, 1, 1);
            arr.SetValue((byte)32, 0, 2, 0);
            arr.SetValue((byte)54, 0, 2, 1);
            arr.SetValue((byte)67, 1, 0, 0);
            arr.SetValue((byte)68, 1, 0, 1);
            arr.SetValue((byte)69, 1, 1, 0);
            arr.SetValue((byte)70, 1, 1, 1);
            arr.SetValue((byte)71, 1, 2, 0);
            arr.SetValue((byte)72, 1, 2, 1);

            if (unknown)
            {
                Setup<Array>(Settings);

                DoSerialize(arr);

                Action<ABSaveSerializer> getElemType = s => s.WriteClosedType(typeof(byte));

                AssertAndGoToStart(GetByteArr(new object[] { getElemType }, (short)GenType.Action, 134, 2, 3, 2, 2, 7, 167, 43, 32, 54, 67, 68, 69, 70, 71, 72));
                CollectionAssert.AreEqual(arr, DoDeserialize<Array>());
            }
            else
            {
                Setup<byte[,,]>(Settings);

                DoSerialize(arr);
                AssertAndGoToStart(0, 2, 3, 2, 0, 2, 7, 167, 43, 32, 54, 67, 68, 69, 70, 71, 72);
                CollectionAssert.AreEqual(arr, DoDeserialize<byte[,,]>());
            }
        }

        [TestMethod]
        [DataRow(false)]
        public void MD_LowerBounds(bool unknown)
        {
            var arr = Array.CreateInstance(typeof(byte), new int[] { 2, 2 }, new int[] { 9, 6 });
            arr.SetValue((byte)2, 9, 6);
            arr.SetValue((byte)7, 9, 7);
            arr.SetValue((byte)167, 10, 6);
            arr.SetValue((byte)43, 10, 7);

            if (unknown)
            {
                Setup<Array>(Settings);

                DoSerialize(arr);

                Action<ABSaveSerializer> getElemType = s => s.WriteClosedType(typeof(byte));

                AssertAndGoToStart(GetByteArr(new object[] { getElemType }, (short)GenType.Action, 196, 2, 2, 9, 6, 2, 7, 167, 43));
                CollectionAssert.AreEqual(arr, DoDeserialize<Array>());
            }
            else
            {
                Setup<byte[,]>(Settings);

                DoSerialize(arr);
                AssertAndGoToStart(0, 130, 2, 9, 6, 0, 2, 7, 167, 43);
                CollectionAssert.AreEqual(arr, DoDeserialize<byte[,]>());
            }
        }
    }
}
