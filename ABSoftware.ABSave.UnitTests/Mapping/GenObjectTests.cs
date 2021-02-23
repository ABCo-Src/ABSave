using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Mapping.Generation;
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
    public class GenObjectTests : MapTestBase
    {
        static void VerifyRuns<TParent, TItem>(MemberAccessor accessor) where TParent : new()
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

        MemberAccessor RunGenerateAccessor(Type type, Type parentType, MemberInfo info)
        {
            var item = Generator.GetMap(type);
            var parent = Generator.GetMap(parentType);

            return GenObject.GenerateAccessor(Generator, ref Generator.Map.GetItemAt(item), ref Generator.Map.GetItemAt(parent), info);
        }

        [TestMethod]
        public void GetAccessor_Field()
        {
            Setup(true);

            var memberInfo = typeof(SimpleClass).GetField(nameof(SimpleClass.Itm1), BindingFlags.NonPublic | BindingFlags.Instance);
            var accessor = RunGenerateAccessor(typeof(bool), typeof(SimpleClass), memberInfo);

            Assert.IsInstanceOfType(accessor.Object1, typeof(FieldInfo));
            Assert.AreEqual(accessor.FieldGetter, accessor.Getter);
            Assert.AreEqual(accessor.FieldSetter, accessor.Setter);

            VerifyRuns<SimpleClass, bool>(accessor);
        }

        [TestMethod]
        public void GetAccessor_ValueTypeParent()
        {
            Setup();

            // Primitive
            var memberInfo = typeof(PropertyStruct).GetProperty(nameof(PropertyStruct.A));

            var accessor = RunGenerateAccessor(typeof(string), typeof(PropertyStruct), memberInfo);

            Assert.IsInstanceOfType(accessor.Object1, typeof(PropertyInfo));
            Assert.AreEqual(accessor.SlowGetter, accessor.Getter);
            Assert.AreEqual(accessor.SlowSetter, accessor.Setter);

            VerifyRuns<PropertyStruct, string>(accessor);
        }

        [TestMethod]
        public void GetAccessor_AllRefTypes()
        {
            Setup();

            // Primitive
            var memberInfo = typeof(PropertyClass).GetProperty(nameof(PropertyClass.A));

            var accessor = RunGenerateAccessor(typeof(string), typeof(PropertyClass), memberInfo);

            Assert.IsInstanceOfType(accessor.Object1, typeof(Func<PropertyClass, string>));
            Assert.IsInstanceOfType(accessor.Object2, typeof(Action<PropertyClass, string>));
            Assert.AreEqual(accessor.AllRefGetter, accessor.Getter);
            Assert.AreEqual(accessor.AllRefSetter, accessor.Setter);

            VerifyRuns<PropertyClass, string>(accessor);
        }

        [TestMethod]
        public void GetAccessor_ValueType_Supported()
        {
            Setup();

            // Primitive
            var memberInfo = typeof(PropertyClass).GetProperty(nameof(PropertyClass.B));

            var accessor = RunGenerateAccessor(typeof(bool), typeof(PropertyClass), memberInfo);

            Assert.IsInstanceOfType(accessor.Object1, typeof(Func<PropertyClass, bool>));
            Assert.IsInstanceOfType(accessor.Object2, typeof(Action<PropertyClass, bool>));
            Assert.AreEqual(accessor.PrimitiveGetter<bool>, accessor.Getter);
            Assert.AreEqual(accessor.PrimitiveSetter<bool>, accessor.Setter);

            VerifyRuns<PropertyClass, bool>(accessor);
        }

        [TestMethod]
        public void GetAccessor_ValueType_Unsupported()
        {
            Setup();

            // Primitive
            var memberInfo = typeof(ClassWithUnspportedValueType).GetProperty(nameof(ClassWithUnspportedValueType.S));

            var accessor = RunGenerateAccessor(typeof(SimpleStruct), typeof(ClassWithUnspportedValueType), memberInfo);

            Assert.IsInstanceOfType(accessor.Object1, typeof(PropertyInfo));
            Assert.AreEqual(accessor.SlowGetter, accessor.Getter);
            Assert.AreEqual(accessor.SlowSetter, accessor.Setter);

            VerifyRuns<ClassWithUnspportedValueType, SimpleStruct>(accessor);
        }

        [TestMethod]
        public void MapObject()
        {
            Setup();

            var properties = new ObjectReflectorInfo()
            {
                UnmappedMembers = 2,
                Members = new LoadOnceList<ObjectReflectorItemInfo>(new ObjectReflectorItemInfo[]
                {
                    new ObjectReflectorItemInfo() { NameKey = nameof(PropertyClass.B), MemberType = typeof(bool), Info = typeof(PropertyClass).GetProperty(nameof(PropertyClass.B)) },
                    new ObjectReflectorItemInfo() { NameKey = nameof(PropertyClass.A), MemberType = typeof(string), Info = typeof(PropertyClass).GetProperty(nameof(PropertyClass.A)) },
                })
                { Length = 2 }
            };

            // Prepare the class for mapping.
            var pos = Generator.CreateItem(typeof(PropertyClass), Map.GenInfo.AllTypes);

            // Run the test
            GenObject.GenerateObject(ref properties, Generator, pos);

            // Assert the results
            ref MapItem item = ref Generator.Map.GetItemAt(pos);
            ref ObjectMapItem objItem = ref MapItem.GetObjectData(ref item);

            Assert.AreEqual(2, objItem.Members.Length);

            Assert.AreEqual(Generator.GetMap(typeof(string)), objItem.Members[0].Map);
            Assert.AreEqual(Generator.GetMap(typeof(bool)), objItem.Members[1].Map);            

            GenObjectReflector.Release(ref properties);
        }
    }
}
