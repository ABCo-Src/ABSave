using ABCo.ABSave.Configuration;
using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABCo.ABSave.UnitTests.Converters
{
    [TestClass]
    public class ObjectTests : ConverterTestBase
    {
        [TestMethod]
        public void EmptyObject()
        {
            Setup<EmptyClass>(ABSaveSettings.ForSpeed);

            DoSerialize(new EmptyClass());
            AssertAndGoToStart(0, 0);

            Assert.IsInstanceOfType(DoDeserialize<EmptyClass>(), typeof(EmptyClass));
        }

        [TestMethod]
        public void Object_NoBaseMembers()
        {
            Setup<NestedClass>(ABSaveSettings.ForSpeed);

            DoSerialize(new NestedClass(100));
            AssertAndGoToStart(0, 0, 0x64, 0xC0, 0x80, SubTypeConverter.OUTPUT_BYTE, 0xC0, SubTypeConverter.OUTPUT_BYTE, 0, 0x64, 9);
            Assert.AreEqual(new NestedClass(100), DoDeserialize<NestedClass>());
        }

        [TestMethod]
        public void Object_WithBaseMembers_Single()
        {
            Setup<MemberInheritingDoubleClass>(ABSaveSettings.ForSpeed,
                new() { { typeof(MemberInheritingDoubleClass), 0 }, { typeof(SingleMemberClass), 0 } });

            DoSerialize(new MemberInheritingDoubleClass(5, 255, 15));
            AssertAndGoToStart(0, 0, 0, 5, 15);
            Assert.AreEqual(new MemberInheritingDoubleClass(5, 0, 15), DoDeserialize<MemberInheritingDoubleClass>());
        }

        [TestMethod]
        public void Object_WithBaseMembers_Double()
        {
            Setup<MemberInheritingDoubleClass>(ABSaveSettings.ForSpeed,
                new() { { typeof(MemberInheritingDoubleClass), 1 }, { typeof(MemberInheritingSingleClass), 1 }});

            DoSerialize(new MemberInheritingDoubleClass(5, 255, 15));
            AssertAndGoToStart(1, 1, 0, 0, 5, 255, 15);
            Assert.AreEqual(new MemberInheritingDoubleClass(5, 255, 15), DoDeserialize<MemberInheritingDoubleClass>());
        }
    }
}
