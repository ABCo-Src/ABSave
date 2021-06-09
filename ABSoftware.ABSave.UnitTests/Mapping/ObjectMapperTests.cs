using ABSoftware.ABSave.Exceptions;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Mapping.Description;
using ABSoftware.ABSave.Mapping.Description.Attributes;
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
    public class ObjectMapperTests : MapTestBase
    {
        static void VerifyRuns<TParent, TItem>(ref MemberAccessor accessor) where TParent : new()
        {
            object obj = new TParent();
            object expected = null;
            if (typeof(TItem) == typeof(int))
                expected = 123;
            else if (typeof(TItem) == typeof(bool))
                expected = true;
            else if (typeof(TItem) == typeof(string))
                expected = "ABC";
            else if (typeof(TItem) == typeof(SimpleStruct))
                expected = new SimpleStruct(true, 172, "d");

            accessor.Setter(obj, expected);

            Assert.AreEqual(expected, accessor.Getter(obj));
        }

        [TestMethod]
        public void GetFieldAccessor()
        {
            Setup();

            var memberInfo = typeof(SimpleClass).GetField(nameof(SimpleClass.Itm1), BindingFlags.NonPublic | BindingFlags.Instance);

            var item = new ObjectMemberSharedInfo();
            MapGenerator.GenerateFieldAccessor(ref item.Accessor, memberInfo);

            Assert.IsInstanceOfType(item.Accessor.Object1, typeof(FieldInfo));
            Assert.AreEqual(MemberAccessorType.Field, item.Accessor.Type);

            VerifyRuns<SimpleClass, bool>(ref item.Accessor);
        }

        void RunGenerateAccessor(ref MemberAccessor dest, Type type, Type parentType, PropertyInfo info)
        {
            var item = Generator.GetMap(type);
            var parent = Generator.GetMap(parentType);

            MapGenerator.GeneratePropertyAccessor(ref dest, item, parent._innerItem, info);
        }

        [TestMethod]
        public void GetPropertyAccessor_ValueTypeParent()
        {
            Setup();

            // Primitive
            var memberInfo = typeof(PropertyStruct).GetProperty(nameof(PropertyStruct.A))!;

            var item = new ObjectMemberSharedInfo();
            RunGenerateAccessor(ref item.Accessor, typeof(string), typeof(PropertyStruct), memberInfo);

            Assert.IsInstanceOfType(item.Accessor.Object1, typeof(PropertyInfo));
            Assert.AreEqual(MemberAccessorType.SlowProperty, item.Accessor.Type);

            VerifyRuns<PropertyStruct, string>(ref item.Accessor);
        }

        [TestMethod]
        public void GetPropertyAccessor_AllRefTypes()
        {
            Setup();

            // Primitive
            var memberInfo = typeof(PropertyClass).GetProperty(nameof(PropertyClass.A))!;

            var item = new ObjectMemberSharedInfo();
            RunGenerateAccessor(ref item.Accessor, typeof(string), typeof(PropertyClass), memberInfo);

            Assert.IsInstanceOfType(item.Accessor.Object1, typeof(MapGenerator.ReferenceGetterDelegate<PropertyClass>));
            Assert.IsInstanceOfType(item.Accessor.Object2, typeof(Action<PropertyClass, string>));
            Assert.AreEqual(MemberAccessorType.AllRefProperty, item.Accessor.Type);

            VerifyRuns<PropertyClass, string>(ref item.Accessor);
        }

        [TestMethod]
        public void GetPropertyAccessor_ValueType_Supported()
        {
            Setup();

            // Primitive
            var memberInfo = typeof(PropertyClass).GetProperty(nameof(PropertyClass.B))!;

            var item = new ObjectMemberSharedInfo();
            RunGenerateAccessor(ref item.Accessor, typeof(bool), typeof(PropertyClass), memberInfo);

            Assert.IsInstanceOfType(item.Accessor.Object1, typeof(Func<PropertyClass, bool>));
            Assert.IsInstanceOfType(item.Accessor.Object2, typeof(Action<PropertyClass, bool>));
            Assert.AreEqual(MemberAccessorType.PrimitiveProperty, item.Accessor.Type);
            Assert.AreEqual(TypeCode.Boolean, item.Accessor.PrimitiveTypeCode);

            VerifyRuns<PropertyClass, bool>(ref item.Accessor);
        }

        [TestMethod]
        public void GetPropertyAccessor_ValueType_Unsupported()
        {
            Setup();

            // Primitive
            var memberInfo = typeof(ClassWithUnspportedForFastAccessorValueType).GetProperty(nameof(ClassWithUnspportedForFastAccessorValueType.S))!;

            var item = new ObjectMemberSharedInfo();
            RunGenerateAccessor(ref item.Accessor, typeof(SimpleStruct), typeof(ClassWithUnspportedForFastAccessorValueType), memberInfo);

            Assert.IsInstanceOfType(item.Accessor.Object1, typeof(PropertyInfo));
            Assert.AreEqual(MemberAccessorType.SlowProperty, item.Accessor.Type);

            VerifyRuns<ClassWithUnspportedForFastAccessorValueType, SimpleStruct>(ref item.Accessor);
        }

        //[TestMethod]
        //public void MapObject_Empty()
        //{
        //    Setup();

        //    var properties = new IntermediateObjInfo()
        //    {
        //        UnmappedCount = 0,
        //        ClassType = typeof(EmptyClass),
        //        HighestVersion = 0,
        //        SortedMembers = null,
        //        RawMembers = Array.Empty<ObjectTranslatedItemInfo>()
        //    };

        //    // Prepare the class for mapping.
        //    var pos = Generator.CreateItem(typeof(EmptyClass), Map.GenInfo.AllTypes);

        //    // Run the test
        //    ObjectMapper.GenerateNewObject(properties, Generator, pos);

        //    // Assert the results
        //    ref MapItem item = ref Generator.Map.GetItemAt(pos);
        //    ref ObjectMapItem objItem = ref item.Main.Object;

        //    Assert.AreEqual(1, objItem.Versions.Count);
        //    Assert.AreEqual(0, objItem.Versions[0].Length);
        //}

        //[TestMethod]
        //public void MapObject_OneVersion()
        //{
        //    Setup();

        //    var properties = new IntermediateObjInfo()
        //    {
        //        UnmappedCount = 2,
        //        ClassType = typeof(PropertyClass),
        //        HighestVersion = 0,
        //        SortedMembers = null,
        //        RawMembers = new ObjectTranslatedItemInfo[]
        //        {
        //            new ObjectTranslatedItemInfo() { Order = 0, MemberType = typeof(string), Info = typeof(PropertyClass).GetProperty(nameof(PropertyClass.A)) },
        //            new ObjectTranslatedItemInfo() { Order = 1, MemberType = typeof(bool), Info = typeof(PropertyClass).GetProperty(nameof(PropertyClass.B)) },
        //        }
        //    };

        //    // Prepare the class for mapping.
        //    var pos = Generator.CreateItem(typeof(PropertyClass), Map.GenInfo.AllTypes);

        //    // Run the test
        //    ObjectMapper.GenerateNewObject(properties, Generator, pos);

        //    // Assert the results
        //    ref MapItem item = ref Generator.Map.GetItemAt(pos);
        //    ref ObjectMapItem objItem = ref item.Main.Object;

        //    Assert.AreEqual(1, objItem.Versions.Count);

        //    var thisVersion = objItem.Versions[0];

        //    Assert.AreEqual(Generator.GetMap(typeof(string)), thisVersion[0].Map);
        //    Assert.AreEqual(Generator.GetMap(typeof(bool)), thisVersion[1].Map);
        //}

        [TestMethod]
        public void GetVersion_NewAndExisting()
        {
            Setup();

            // Prepare the class for mapping.
            var item = (ObjectMapItem)Generator.GetMap(typeof(VersionedPropertyClass))._innerItem;

            // Create a new version - version 1.
            {
                var info = Map.GetMembersForVersion(item, 1);

                Assert.AreEqual(3, info.Members.Length);
                Assert.AreEqual(Generator.GetMap(typeof(DateTime)), info.Members[0].Map);
                Assert.AreEqual(Generator.GetMap(typeof(bool)), info.Members[1].Map);
                Assert.AreEqual(Generator.GetMap(typeof(int)), info.Members[2].Map);
                Assert.AreEqual(SaveInheritanceMode.Key, info.InheritanceInfo!.Mode);

                var infoAgain = Map.GetMembersForVersion(item, 1);
                Assert.AreEqual(info, infoAgain);
            }

            // Create a new version - version 0.
            {
                var info = Map.GetMembersForVersion(item, 0);

                Assert.AreEqual(1, info.Members.Length);
                Assert.AreEqual(Generator.GetMap(typeof(DateTime)), info.Members[0].Map);
                Assert.AreEqual(SaveInheritanceMode.Key, info.InheritanceInfo!.Mode);

                var infoAgain = Map.GetMembersForVersion(item, 0);
                Assert.AreEqual(info, infoAgain);
            }

            // Create a new version - version 2.
            {
                var info = Map.GetMembersForVersion(item, 2);

                Assert.AreEqual(3, info.Members.Length);
                Assert.AreEqual(Generator.GetMap(typeof(DateTime)), info.Members[0].Map);
                Assert.AreEqual(Generator.GetMap(typeof(int)), info.Members[1].Map);
                Assert.AreEqual(Generator.GetMap(typeof(long)), info.Members[2].Map);
                Assert.IsNull(info.InheritanceInfo);

                var infoAgain = Map.GetMembersForVersion(item, 2);
                Assert.AreEqual(info, infoAgain);
            }

            // Create a new version - version 3.
            {
                var info = Map.GetMembersForVersion(item, 3);

                // The same members as version 2
                Assert.AreEqual(3, info.Members.Length);
                Assert.AreEqual(Generator.GetMap(typeof(DateTime)), info.Members[0].Map);
                Assert.AreEqual(Generator.GetMap(typeof(int)), info.Members[1].Map);
                Assert.AreEqual(Generator.GetMap(typeof(long)), info.Members[2].Map);

                // No inheritance
                Assert.AreEqual(SaveInheritanceMode.IndexOrKey, info.InheritanceInfo!.Mode);

                var infoAgain = Map.GetMembersForVersion(item, 3);
                Assert.AreEqual(info, infoAgain);
            }
        }

        [TestMethod]
        public void GetVersion_Invalid()
        {
            Setup();

            var pos = Generator.GetMap(typeof(VersionedPropertyClass));

            Assert.ThrowsException<UnsupportedVersionException>(() => Map.GetMembersForVersion((ObjectMapItem)pos._innerItem, 4));
        }
    }
}
