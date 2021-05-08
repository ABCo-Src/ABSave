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
        [TestMethod]
        public void FillMainInfo_CorrectContext_Ordered()
        {
            Setup();

            var dest = new IntermediateObjInfo();

            var ctx = new IntermediateObjInfoMapper.TranslationContext(dest);
            ctx.CurrentMembers.Initialize();

            ref ObjectTranslatedItemInfo info = ref ctx.CurrentMembers.CreateAndGet();
            info.MemberType = typeof(string);
            IntermediateObjInfoMapper.FillMainInfo(ref ctx, ref info, 3, 6, 7);

            info = ref ctx.CurrentMembers.CreateAndGet();
            info.MemberType = typeof(int);
            IntermediateObjInfoMapper.FillMainInfo(ref ctx, ref info, 5, 5, 34);

            info = ref ctx.CurrentMembers.CreateAndGet();
            info.MemberType = typeof(int);
            IntermediateObjInfoMapper.FillMainInfo(ref ctx, ref info, 9, 5, 59);

            Assert.AreEqual(ctx.TranslationCurrentOrderInfo, 9);
            Assert.AreEqual(dest.HighestVersion, 59);
        }

        [TestMethod]
        public void FillMainInfo_CorrectContext_Unordered()
        {
            Setup();

            var dest = new IntermediateObjInfo();

            var ctx = new IntermediateObjInfoMapper.TranslationContext(dest);
            ctx.CurrentMembers.Initialize();

            ref ObjectTranslatedItemInfo info = ref ctx.CurrentMembers.CreateAndGet();
            info.MemberType = typeof(string);
            IntermediateObjInfoMapper.FillMainInfo(ref ctx, ref info, 9, 6, 7);

            info = ref ctx.CurrentMembers.CreateAndGet();
            info.MemberType = typeof(int);
            IntermediateObjInfoMapper.FillMainInfo(ref ctx, ref info, 5, 5, 34);

            info = ref ctx.CurrentMembers.CreateAndGet();
            info.MemberType = typeof(int);
            IntermediateObjInfoMapper.FillMainInfo(ref ctx, ref info, 7, 5, 103);

            Assert.AreEqual(ctx.TranslationCurrentOrderInfo, -1);
            Assert.AreEqual(dest.HighestVersion, 103);
        }

        [TestMethod]
        public void OrderMembers()
        {
            Setup();

            var dest = new IntermediateObjInfo();
            var ctx = new IntermediateObjInfoMapper.TranslationContext(dest);

            ctx.CurrentMembers.Initialize();
            ref ObjectTranslatedItemInfo item = ref ctx.CurrentMembers.CreateAndGet();
            (item.Order, item.MemberType) = (3, typeof(string));

            item = ref ctx.CurrentMembers.CreateAndGet();
            (item.Order, item.MemberType) = (15, typeof(string));

            item = ref ctx.CurrentMembers.CreateAndGet();
            (item.Order, item.MemberType) = (7, typeof(int));

            dest.RawMembers = ctx.CurrentMembers.ReleaseAndGetArray();
            IntermediateObjInfoMapper.OrderMembers(ref ctx);

            Assert.AreEqual(0, dest.SortedMembers[0].Index);
            Assert.AreEqual(2, dest.SortedMembers[1].Index);
            Assert.AreEqual(1, dest.SortedMembers[2].Index);
        }

        [TestMethod]
        public void NoAttribute()
        {
            Setup();

            Assert.ThrowsException<UnserializableType>(() => IntermediateObjInfoMapper.CreateInfo(typeof(UnserializableClass), Generator));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Fields(bool isValueTypeParent)
        {
            Setup();

            var strMap = Generator.GetMap(typeof(string));
            var info = IntermediateObjInfoMapper.CreateInfo(isValueTypeParent ? typeof(SimpleStruct) : typeof(SimpleClass), Generator);

            Assert.AreEqual(2, info.UnmappedCount);
            Assert.AreEqual(3, info.RawMembers.Length);

            var iterator = new IntermediateObjInfo.MemberIterator(info);

            int i = 0;
            do
            {
                ref ObjectTranslatedItemInfo item = ref iterator.GetCurrent();

                bool isItm3 = item.Order == 2;

                Assert.AreEqual(isItm3 ? strMap : null, item.ExistingMap);
                Assert.AreEqual(null, item.Accessor);
                Assert.IsInstanceOfType(item.Info, typeof(FieldInfo));

                Type expectedType = i switch
                {
                    0 => typeof(bool),
                    1 => typeof(int),
                    2 => typeof(string),
                    _ => throw new Exception("Incorrect key")
                };

                Assert.AreEqual(expectedType, item.MemberType);
                i++;
            }
            while (iterator.MoveNext());

            //IntermediateObjInfoMapper.Release(info);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Properties(bool isValueTypeParent)
        {
            Setup();

            var info = IntermediateObjInfoMapper.CreateInfo(isValueTypeParent ? typeof(PropertyStruct) : typeof(PropertyClass), Generator);

            try
            {
                Assert.AreEqual(2, info.UnmappedCount);
                Assert.AreEqual(2, info.RawMembers.Length);

                var iterator = new IntermediateObjInfo.MemberIterator(info);

                int i = 0;
                do
                {
                    ref ObjectTranslatedItemInfo item = ref iterator.GetCurrent();

                    Assert.AreEqual(null, item.ExistingMap);
                    Assert.AreEqual(null, item.Accessor);
                    Assert.IsInstanceOfType(item.Info, typeof(PropertyInfo));

                    Type expectedType = i switch
                    {
                        0 => typeof(string),
                        1 => typeof(bool),
                        _ => throw new Exception("Invalid key")
                    };

                    Assert.AreEqual(expectedType, item.MemberType);
                    i++;
                }
                while (iterator.MoveNext());

            }
            finally 
            {
                IntermediateObjInfoMapper.Release(info);
            }
        }
        
        [TestMethod]
        public void Properties_Unordered()
        {
            Setup();

            var info = IntermediateObjInfoMapper.CreateInfo(typeof(UnorderedPropertyClass), Generator);

            try
            {
                var iterator = new IntermediateObjInfo.MemberIterator(info);

                int i = 0;
                do
                {
                    ref ObjectTranslatedItemInfo item = ref iterator.GetCurrent();

                    Type expectedType = i switch
                    {
                        0 => typeof(bool),
                        1 => typeof(string),
                        _ => throw new Exception("Invalid key")
                    };

                    Assert.AreEqual(expectedType, item.MemberType);
                    i++;
                }
                while (iterator.MoveNext());
            }
            finally
            {
                IntermediateObjInfoMapper.Release(info);
            }
        }
    }
}
