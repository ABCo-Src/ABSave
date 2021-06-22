using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping.Generation;
using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ABCo.ABSave.UnitTests.Mapping
{
    [TestClass]
    public class IntermediateObjInfoMapperTests : MapTestBase
    {
        public int A;
        public int B;
        public int C;

        [TestMethod]
        public void FillMainInfo_CorrectValues()
        {
            ObjectIntermediateItem info = new ObjectIntermediateItem();
            MapGenerator.FillMainInfo(info, 3, 6, -1);

            Assert.AreEqual(3, info.Order);
            Assert.AreEqual(6u, info.StartVer);
            Assert.AreEqual(uint.MaxValue, info.EndVer);
        }

        [TestMethod]
        public void FillMainInfo_CorrectHighestVersion_NoCustomHighs()
        {
            var ctx = new MapGenerator();            

            ObjectIntermediateItem info = new ObjectIntermediateItem();
            MapGenerator.FillMainInfo(info, 3, 6, -1);
            ctx.UpdateContextFromItem(info);

            MapGenerator.FillMainInfo(info, 5, 8, -1);
            ctx.UpdateContextFromItem(info);

            MapGenerator.FillMainInfo(info, 9, 11, -1);
            ctx.UpdateContextFromItem(info);

            Assert.AreEqual(9, ctx._intermediateContext.TranslationCurrentOrderInfo);
            Assert.AreEqual(11u, ctx._intermediateContext.HighestVersion);
        }

        [TestMethod]
        public void FillMainInfo_CorrectHighestVersion_CustomHighs()
        {
            var ctx = new MapGenerator();

            ObjectIntermediateItem info = new ObjectIntermediateItem();
            MapGenerator.FillMainInfo(info, 3, 6, 7);
            ctx.UpdateContextFromItem(info);

            MapGenerator.FillMainInfo(info, 5, 8, 34);
            ctx.UpdateContextFromItem(info);

            MapGenerator.FillMainInfo(info, 9, 11, 59);
            ctx.UpdateContextFromItem(info);

            Assert.AreEqual(9, ctx._intermediateContext.TranslationCurrentOrderInfo);
            Assert.AreEqual(59u, ctx._intermediateContext.HighestVersion);
        }

        [TestMethod]
        public void FillMainInfo_CorrectHighestVersion_Same()
        {
            Setup();

            var ctx = new MapGenerator();

            ObjectIntermediateItem info = new ObjectIntermediateItem();
            MapGenerator.FillMainInfo(info, 3, 0, -1);
            ctx.UpdateContextFromItem(info);

            MapGenerator.FillMainInfo(info, 5, 0, -1);
            ctx.UpdateContextFromItem(info);

            MapGenerator.FillMainInfo(info, 9, 0, -1);
            ctx.UpdateContextFromItem(info);

            Assert.AreEqual(9, ctx._intermediateContext.TranslationCurrentOrderInfo);
            Assert.AreEqual(0u, ctx._intermediateContext.HighestVersion);
        }

        [TestMethod]
        public void FillMainInfo_CorrectOrderInfo_Ordered()
        {
            Setup();

            var ctx = new MapGenerator();

            ObjectIntermediateItem info = new ObjectIntermediateItem();

            MapGenerator.FillMainInfo(info, 3, 0, -1);
            ctx.UpdateContextFromItem(info);

            MapGenerator.FillMainInfo(info, 5, 0, -1);
            ctx.UpdateContextFromItem(info);

            MapGenerator.FillMainInfo(info, 9, 0, -1);
            ctx.UpdateContextFromItem(info);

            Assert.AreEqual(9, ctx._intermediateContext.TranslationCurrentOrderInfo);
        }

        [TestMethod]
        public void FillMainInfo_CorrectContext_Unordered()
        {
            Setup();

            var ctx = new MapGenerator();

            ObjectIntermediateItem info = new ObjectIntermediateItem();

            MapGenerator.FillMainInfo(info, 9, 0, -1);
            ctx.UpdateContextFromItem(info);

            MapGenerator.FillMainInfo(info, 5, 0, -1);
            ctx.UpdateContextFromItem(info);

            MapGenerator.FillMainInfo(info, 7, 0, -1);
            ctx.UpdateContextFromItem(info);

            Assert.AreEqual(-1, ctx._intermediateContext.TranslationCurrentOrderInfo);
        }

        [TestMethod]
        public void ProcessItemAttributes_NoAttribute()
        {
            Setup();

            var ctx = Generator.CurrentReflectionMapper;
            var member = typeof(ClassWithSkippableItem).GetProperty(nameof(ClassWithSkippableItem.Skippable));
            ctx.PrepareBufferForSize(3);

            ObjectIntermediateItem item = null;
            int count = 0;

            MapGenerator.ReflectionMapper.ProcessMemberAttributes(member, ref item, ref count);
            Assert.AreEqual(0, count);
            Assert.IsNull(item);
        }

        [TestMethod]
        public void ProcessItemAttributes_Valid()
        {
            Setup();

            var ctx = Generator.CurrentReflectionMapper;
            var member = typeof(VersionedClass).GetProperty(nameof(VersionedClass.B));
            ctx.PrepareBufferForSize(3);

            ObjectIntermediateItem item = null;
            int count = 0;

            MapGenerator.ReflectionMapper.ProcessMemberAttributes(member, ref item, ref count);
            Assert.AreEqual(1, count);
            Assert.AreEqual(1, item.Order);
            Assert.AreEqual(1u, item.StartVer);
            Assert.AreEqual(1u, item.EndVer);
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
        public void Class_NoAttribute()
        {
            Setup();

            Assert.ThrowsException<UnserializableTypeException>(() =>
            {
                var info = new ObjectIntermediateInfo();
                Generator.CreateIntermediateObjectInfo(typeof(UnserializableClass), ref info);
            });
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Fields(bool isValueTypeParent)
        {
            Setup();
            TestFields(isValueTypeParent);

            //IntermediateObjInfoMapper.Release(info);
        }

        void TestFields(bool isValueTypeParent)
        {
            var info = new ObjectIntermediateInfo();
            uint version = Generator.CreateIntermediateObjectInfo(isValueTypeParent ? typeof(FieldStruct) : typeof(FieldClass), ref info);

            Assert.AreEqual(0u, version);
            Assert.AreEqual(2, info.RawMembers.Length);

            for (int i = 0; i < info.RawMembers.Length; i++)
            {
                Assert.IsFalse(info.RawMembers[i].IsProcessed);
                Assert.IsInstanceOfType(info.RawMembers[i].Details.Unprocessed, typeof(FieldInfo));

                Type expectedType = i switch
                {
                    0 => typeof(string),
                    1 => typeof(bool),
                    _ => throw new Exception("Incorrect key")
                };

                Assert.AreEqual(expectedType, ((FieldInfo)info.RawMembers[i].Details.Unprocessed).FieldType);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Properties(bool isValueTypeParent)
        {
            Setup();
            TestProperties(isValueTypeParent);
        }

        void TestProperties(bool isValueTypeParent)
        {
            var info = new ObjectIntermediateInfo();
            uint version = Generator.CreateIntermediateObjectInfo(isValueTypeParent ? typeof(AllPrimitiveStruct) : typeof(AllPrimitiveClass), ref info);

            Assert.AreEqual(0u, version);
            Assert.AreEqual(3, info.RawMembers.Length);

            for (int i = 0; i < info.RawMembers.Length; i++)
            {
                Assert.IsFalse(info.RawMembers[i].IsProcessed);
                Assert.IsInstanceOfType(info.RawMembers[i].Details.Unprocessed, typeof(PropertyInfo));

                Type expectedType = i switch
                {
                    0 => typeof(bool),
                    1 => typeof(int),
                    2 => typeof(string),
                    _ => throw new Exception("Invalid key")
                };

                Assert.AreEqual(expectedType, ((PropertyInfo)info.RawMembers[i].Details.Unprocessed)!.PropertyType);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void FieldsAndProperties_SmallToLarge_SharedGenerator(bool isValueTypeParent)
        {
            // If we re-use the same map generator and the same buffer within it but for different
            // numbers of members see if it still works.
            Setup();

            TestProperties(isValueTypeParent);
            TestFields(isValueTypeParent);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void FieldsAndProperties_LargeToSmall_SharedGenerator(bool isValueTypeParent)
        {
            // If we re-use the same map generator and the same buffer within it but for different
            // numbers of members see if it still works.
            Setup();

            TestFields(isValueTypeParent);
            TestProperties(isValueTypeParent);
        }

        [TestMethod]
        public void Properties_Unordered()
        {
            Setup();

            var info = new ObjectIntermediateInfo();
            uint version = Generator.CreateIntermediateObjectInfo(typeof(UnorderedClass), ref info);

            Assert.AreEqual(0u, version);

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
