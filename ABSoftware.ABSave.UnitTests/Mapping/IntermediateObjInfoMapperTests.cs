using ABSoftware.ABSave.Exceptions;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping.Generation;
using ABSoftware.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ABSoftware.ABSave.UnitTests.Mapping
{
    [TestClass]
    public class IntermediateObjInfoMapperTests : MapTestBase
    {
        public int A;
        public int B;
        public int C;

        [TestMethod]
        public void FillMainInfo_CorrectHighestVersion_NoCustomHighs()
        {
            var ctx = new MapGenerator.TranslationContext(typeof(IntermediateObjInfoMapperTests));

            ObjectIntermediateItem info = new ObjectIntermediateItem();
            MapGenerator.FillMainInfo(ref ctx, info, 3, 6, -1);
            MapGenerator.FillMainInfo(ref ctx, info, 5, 8, -1);
            MapGenerator.FillMainInfo(ref ctx, info, 9, 11, -1);

            Assert.AreEqual(ctx.TranslationCurrentOrderInfo, 9);
            Assert.AreEqual(ctx.HighestVersion, 11);
        }

        [TestMethod]
        public void FillMainInfo_CorrectHighestVersion_CustomHighs()
        {
            var ctx = new MapGenerator.TranslationContext(typeof(IntermediateObjInfoMapperTests));

            ObjectIntermediateItem info = new ObjectIntermediateItem();
            MapGenerator.FillMainInfo(ref ctx, info, 3, 6, 7);
            MapGenerator.FillMainInfo(ref ctx, info, 5, 8, 34);
            MapGenerator.FillMainInfo(ref ctx, info, 9, 11, 59);

            Assert.AreEqual(ctx.TranslationCurrentOrderInfo, 9);
            Assert.AreEqual(ctx.HighestVersion, 59);
        }

        [TestMethod]
        public void FillMainInfo_CorrectHighestVersion_Same()
        {
            Setup();

            var ctx = new MapGenerator.TranslationContext(typeof(IntermediateObjInfoMapperTests));

            ObjectIntermediateItem info = new ObjectIntermediateItem();
            MapGenerator.FillMainInfo(ref ctx, info, 3, 0, -1);
            MapGenerator.FillMainInfo(ref ctx, info, 5, 0, -1);
            MapGenerator.FillMainInfo(ref ctx, info, 9, 0, -1);

            Assert.AreEqual(ctx.TranslationCurrentOrderInfo, 9);
            Assert.AreEqual(ctx.HighestVersion, 0);
        }

        [TestMethod]
        public void FillMainInfo_CorrectOrderInfo_Ordered()
        {
            Setup();

            var ctx = new MapGenerator.TranslationContext(typeof(IntermediateObjInfoMapperTests));

            ObjectIntermediateItem info = new ObjectIntermediateItem();
            MapGenerator.FillMainInfo(ref ctx, info, 3, 0, -1);
            MapGenerator.FillMainInfo(ref ctx, info, 5, 0, -1);
            MapGenerator.FillMainInfo(ref ctx, info, 9, 0, -1);

            Assert.AreEqual(ctx.TranslationCurrentOrderInfo, 9);
        }

        [TestMethod]
        public void FillMainInfo_CorrectContext_Unordered()
        {
            Setup();

            var ctx = new MapGenerator.TranslationContext(typeof(IntermediateObjInfoMapperTests));

            ObjectIntermediateItem info = new ObjectIntermediateItem();
            MapGenerator.FillMainInfo(ref ctx, info, 9, 0, -1);
            MapGenerator.FillMainInfo(ref ctx, info, 5, 0, -1);
            MapGenerator.FillMainInfo(ref ctx, info, 7, 0, -1);

            Assert.AreEqual(ctx.TranslationCurrentOrderInfo, -1);
        }

        //[TestMethod]
        //public void OrderMembers()
        //{
        //    Setup();

        //    var dest = new IntermediateObjInfo();
        //    var ctx = new IntermediateObjInfoMapper.TranslationContext(typeof(IntermediateObjInfoMapperTests));

        //    ObjectIntermediateItem item = new ObjectIntermediateItem()
        //    {
        //        Order = 3
        //    };
        //    ctx.CurrentMembers.Add(item);

        //    item = new ObjectIntermediateItem()
        //    {
        //        Order = 15
        //    };
        //    ctx.CurrentMembers.Add(item);

        //    item = new ObjectIntermediateItem()
        //    {
        //        Order = 7
        //    };
        //    ctx.CurrentMembers.Add(item);

        //    IntermediateObjInfoMapper.(ref ctx);

        //    Assert.AreEqual(0, dest.SortedMembers[0].Index);
        //    Assert.AreEqual(2, dest.SortedMembers[1].Index);
        //    Assert.AreEqual(1, dest.SortedMembers[2].Index);
        //}

        [TestMethod]
        public void NoAttribute()
        {
            Setup();

            Assert.ThrowsException<UnserializableType>(() => Generator.CreateIntermediateObjectInfo(typeof(UnserializableClass)));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Fields(bool isValueTypeParent)
        {
            Setup();

            var info = Generator.CreateIntermediateObjectInfo(isValueTypeParent ? typeof(SimpleStruct) : typeof(SimpleClass));

            Assert.AreEqual(3, info.RawMembers.Length);
            
            for (int i = 0; i < info.RawMembers.Length; i++)
            {
                Assert.IsFalse(info.RawMembers[i].IsProcessed);
                Assert.IsInstanceOfType(info.RawMembers[i].Details.Unprocessed, typeof(FieldInfo));

                Type expectedType = i switch
                {
                    0 => typeof(bool),
                    1 => typeof(int),
                    2 => typeof(string),
                    _ => throw new Exception("Incorrect key")
                };

                Assert.AreEqual(expectedType, ((FieldInfo)info.RawMembers[i].Details.Unprocessed).FieldType);
            }

            //IntermediateObjInfoMapper.Release(info);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Properties(bool isValueTypeParent)
        {
            Setup();

            var info = Generator.CreateIntermediateObjectInfo(isValueTypeParent ? typeof(PropertyStruct) : typeof(PropertyClass));

            Assert.AreEqual(0, info.HighestVersion);
            Assert.AreEqual(2, info.RawMembers.Length);

            for (int i = 0; i < info.RawMembers.Length; i++)
            {
                Assert.IsFalse(info.RawMembers[i].IsProcessed);
                Assert.IsInstanceOfType(info.RawMembers[i].Details.Unprocessed, typeof(PropertyInfo));

                Type expectedType = i switch
                {
                    0 => typeof(string),
                    1 => typeof(bool),
                    _ => throw new Exception("Invalid key")
                };

                Assert.AreEqual(expectedType, ((PropertyInfo)info.RawMembers[i].Details.Unprocessed)!.PropertyType);
            }
        }
        
        [TestMethod]
        public void Properties_Unordered()
        {
            Setup();

            var info = Generator.CreateIntermediateObjectInfo(typeof(UnorderedPropertyClass));

            for (int i = 0; i < info.RawMembers.Length; i++)
            {
                Assert.IsFalse(info.RawMembers[i].IsProcessed);
                Assert.IsInstanceOfType(info.RawMembers[i].Details.Unprocessed, typeof(PropertyInfo));

                Type expectedType = i switch
                {
                    0 => typeof(bool),
                    1 => typeof(string),
                    _ => throw new Exception("Invalid key")
                };

                Assert.AreEqual(expectedType, ((PropertyInfo)info.RawMembers[i].Details.Unprocessed).PropertyType);
                i++;
            }
        }
    }
}
