﻿using ABCo.ABSave.Serialization.Converters;
using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description;
using ABCo.ABSave.Mapping.Generation.Object;
using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;
using ABCo.ABSave.Mapping.Description.Attributes;

namespace ABCo.ABSave.UnitTests.Mapping
{
    [TestClass]
    public class ObjectMapperTests : MapTestBase
    {
        static void VerifyRuns<TParent, TItem>(ref MemberAccessor accessor) where TParent : new()
        {
            object obj = new TParent();
            object expected = null;
            if (typeof(TItem) == typeof(int))
            {
                expected = 123;
            }

            if (typeof(TItem) == typeof(byte))
            {
                expected = (byte)123;
            }
            else if (typeof(TItem) == typeof(bool))
            {
                expected = true;
            }
            else if (typeof(TItem) == typeof(string))
            {
                expected = "ABC";
            }
            else if (typeof(TItem) == typeof(AllPrimitiveStruct))
            {
                expected = new AllPrimitiveStruct(true, 172, "d");
            }

            accessor.Setter(obj, expected);

            Assert.AreEqual(expected, accessor.Getter(obj));
        }

        [TestMethod]
        public void GetFieldAccessor()
        {
            Setup();

            var memberInfo = typeof(FieldClass).GetField(nameof(FieldClass.A));

            var item = new ObjectMemberSharedInfo();
            MemberAccessorGenerator.GenerateFieldAccessor(ref item.Accessor, memberInfo);

            Assert.IsInstanceOfType(item.Accessor.Object1, typeof(FieldInfo));
            Assert.AreEqual(MemberAccessorType.Field, item.Accessor.Type);

            VerifyRuns<FieldClass, string>(ref item.Accessor);
        }

        [TestMethod]
        public void GenerateFieldAccessor_ReadOnlyField_ThrowsException()
        {
	        Setup();

	        var memberInfo = typeof(ReadonlyFieldClass).GetField(nameof(ReadonlyFieldClass.A));

	        var item = new ObjectMemberSharedInfo();
	        Assert.ThrowsException<UnsupportedMemberException>(() => MemberAccessorGenerator.GenerateFieldAccessor(ref item.Accessor, memberInfo));
        }

        [TestMethod]
        public void GeneratePropertyAccessor_ReadonlyProperty_ThrowsException()
        {
	        Setup();

	        var propertyInfo = typeof(ReadonlyPropertyClass).GetProperty(nameof(ReadonlyPropertyClass.A));
	        
	        var item = new ObjectMemberSharedInfo();

            Assert.ThrowsException<UnsupportedMemberException>(() => RunGenerateAccessor(ref item.Accessor, typeof(int), typeof(ReadonlyPropertyClass), propertyInfo));
        }

        void RunGenerateAccessor(ref MemberAccessor dest, Type type, Type parentType, PropertyInfo info)
        {
            var item = Generator.GetMap(type);
            var parent = Generator.GetMap(parentType);

            MemberAccessorGenerator.DoGeneratePropertyAccessor(ref dest, item, parent.Converter, info);
        }

        [TestMethod]
        public void GetPropertyAccessor_ValueTypeParent()
        {
            Setup();

            // Primitive
            var memberInfo = typeof(AllPrimitiveStruct).GetProperty(nameof(AllPrimitiveStruct.C))!;

            var item = new ObjectMemberSharedInfo();
            RunGenerateAccessor(ref item.Accessor, typeof(string), typeof(AllPrimitiveStruct), memberInfo);

            Assert.IsInstanceOfType(item.Accessor.Object1, typeof(PropertyInfo));
            Assert.AreEqual(MemberAccessorType.SlowProperty, item.Accessor.Type);

            VerifyRuns<AllPrimitiveStruct, string>(ref item.Accessor);
        }

        [TestMethod]
        public void GetPropertyAccessor_AllRefTypes()
        {
            Setup();

            // Primitive
            var memberInfo = typeof(NestedClass).GetProperty(nameof(NestedClass.B))!;

            var item = new ObjectMemberSharedInfo();
            RunGenerateAccessor(ref item.Accessor, typeof(SubWithHeader), typeof(NestedClass), memberInfo);

            Assert.IsInstanceOfType(item.Accessor.Object1, typeof(MemberAccessorGenerator.ReferenceGetterDelegate<NestedClass>));
            Assert.IsInstanceOfType(item.Accessor.Object2, typeof(Action<NestedClass, SubWithHeader>));
            Assert.AreEqual(MemberAccessorType.AllRefProperty, item.Accessor.Type);

            VerifyRuns<NestedClass, SubWithHeader>(ref item.Accessor);
        }

        [TestMethod]
        public void GetPropertyAccessor_ValueType_Supported()
        {
            Setup();

            // Primitive
            var memberInfo = typeof(NestedClass).GetProperty(nameof(NestedClass.A))!;

            var item = new ObjectMemberSharedInfo();
            RunGenerateAccessor(ref item.Accessor, typeof(byte), typeof(NestedClass), memberInfo);

            Assert.IsInstanceOfType(item.Accessor.Object1, typeof(Func<NestedClass, byte>));
            Assert.IsInstanceOfType(item.Accessor.Object2, typeof(Action<NestedClass, byte>));
            Assert.AreEqual(MemberAccessorType.PrimitiveProperty, item.Accessor.Type);
            Assert.AreEqual(TypeCode.Byte, item.Accessor.PrimitiveTypeCode);

            VerifyRuns<NestedClass, byte>(ref item.Accessor);
        }

        [TestMethod]
        public void GetPropertyAccessor_ValueType_Unsupported()
        {
            Setup();

            // Primitive
            var memberInfo = typeof(ClassWithUnspportedForFastAccessorValueType).GetProperty(nameof(ClassWithUnspportedForFastAccessorValueType.S))!;

            var item = new ObjectMemberSharedInfo();
            RunGenerateAccessor(ref item.Accessor, typeof(AllPrimitiveStruct), typeof(ClassWithUnspportedForFastAccessorValueType), memberInfo);

            Assert.IsInstanceOfType(item.Accessor.Object1, typeof(PropertyInfo));
            Assert.AreEqual(MemberAccessorType.SlowProperty, item.Accessor.Type);

            VerifyRuns<ClassWithUnspportedForFastAccessorValueType, AllPrimitiveStruct>(ref item.Accessor);
        }

        [TestMethod]
        public void GetVersion_NewAndExisting()
        {
            Setup();

            // Prepare the class for mapping.
            var item = (ObjectConverter)Generator.GetMap(typeof(VersionedClass)).Converter;

            // Create a new version - version 1.
            {
                var info = (ObjectConverter.ObjectVersionInfo)Map.GetVersionInfo(item, 1);

                Assert.AreEqual(3, info.Members.Length);
                Assert.AreEqual(Generator.GetMap(typeof(DateTime)), info.Members[0].Map);
                Assert.AreEqual(Generator.GetMap(typeof(bool)), info.Members[1].Map);
                Assert.AreEqual(Generator.GetMap(typeof(int)), info.Members[2].Map);
                Assert.AreEqual(SaveInheritanceMode.Key, info._inheritanceInfo!.Mode);

                var infoAgain = Map.GetVersionInfo(item, 1);
                Assert.AreEqual(info, infoAgain);
            }

            // Create a new version - version 0.
            {
                var info = (ObjectConverter.ObjectVersionInfo)Map.GetVersionInfo(item, 0);

                Assert.AreEqual(1, info.Members.Length);
                Assert.AreEqual(Generator.GetMap(typeof(DateTime)), info.Members[0].Map);
                Assert.AreEqual(SaveInheritanceMode.Key, info._inheritanceInfo!.Mode);

                var infoAgain = Map.GetVersionInfo(item, 0);
                Assert.AreEqual(info, infoAgain);
            }

            // Create a new version - version 2.
            {
                var info = (ObjectConverter.ObjectVersionInfo)Map.GetVersionInfo(item, 2);

                Assert.AreEqual(3, info.Members.Length);
                Assert.AreEqual(Generator.GetMap(typeof(DateTime)), info.Members[0].Map);
                Assert.AreEqual(Generator.GetMap(typeof(int)), info.Members[1].Map);
                Assert.AreEqual(Generator.GetMap(typeof(long)), info.Members[2].Map);
                Assert.IsNull(info._inheritanceInfo);

                var infoAgain = Map.GetVersionInfo(item, 2);
                Assert.AreEqual(info, infoAgain);
            }

            // Create a new version - version 3.
            {
                var info = (ObjectConverter.ObjectVersionInfo)Map.GetVersionInfo(item, 3);

                // The same members as version 2
                Assert.AreEqual(3, info.Members.Length);
                Assert.AreEqual(Generator.GetMap(typeof(DateTime)), info.Members[0].Map);
                Assert.AreEqual(Generator.GetMap(typeof(int)), info.Members[1].Map);
                Assert.AreEqual(Generator.GetMap(typeof(long)), info.Members[2].Map);

                // No inheritance
                Assert.AreEqual(SaveInheritanceMode.IndexOrKey, info._inheritanceInfo!.Mode);

                var infoAgain = Map.GetVersionInfo(item, 3);
                Assert.AreEqual(info, infoAgain);
            }
        }

        [TestMethod]
        public void GetVersion_Invalid()
        {
            Setup();

            var pos = Generator.GetMap(typeof(VersionedClass));

            Assert.ThrowsException<UnsupportedVersionException>(() => Map.GetVersionInfo((ObjectConverter)pos.Converter, 4));
        }

        [TestMethod]
        public void GetVersion_New_WithFieldsAndProperties()
        {
            Setup();
            var converter = Generator.GetMap(typeof(MixedClass)).Converter;
            var info = (ObjectConverter.ObjectVersionInfo)Map.GetVersionInfo(converter, 0);

            Assert.AreEqual(3, info.Members.Length);
            Assert.AreEqual(Generator.GetMap(typeof(int)), info.Members[0].Map);
            Assert.AreEqual(Generator.GetMap(typeof(DateTime)), info.Members[1].Map);
            Assert.AreEqual(Generator.GetMap(typeof(long)), info.Members[2].Map);
        }
    }

    [SaveMembers(SaveMembersMode.Properties | SaveMembersMode.Fields)]
    class MixedClass
    {
        [Save(0)]
        public int A = 0;

        [Save(5)]
        public DateTime B { get; set; }

        [Save(10)]
        public long C = 0;
    }

    [SaveMembers(SaveMembersMode.Properties | SaveMembersMode.Fields)]
    class ReadonlyFieldClass
    {
	    [Save(0)] public readonly int A = 0;
    }

    [SaveMembers()]
    class ReadonlyPropertyClass
    {
	    [Save(0)] public int A { get; } = 0;
    }
}
