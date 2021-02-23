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
    public class GenObjectReflectorTests : MapTestBase
    {
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Class_Fields(bool isValueTypeParent)
        {
            Setup(true);

            var info = new ObjectReflectorInfo();
            var strMap = Generator.GetMap(typeof(string));

            GenObjectReflector.GetAllMembersInfo(ref info, isValueTypeParent ? typeof(SimpleStruct) : typeof(SimpleClass), Generator);

            Assert.AreEqual(2, info.UnmappedMembers);
            Assert.AreEqual(3, info.Members.Length);

            for (int i = 0; i < 3; i++)
            {
                bool isItm3 = info.Members[i].NameKey == nameof(SimpleClass.Itm3);
                
                Assert.AreEqual(isItm3 ? strMap : null, info.Members[i].ExistingMap);
                Assert.AreEqual(null, info.Members[i].Accessor);
                Assert.IsInstanceOfType(info.Members[i].Info, typeof(FieldInfo));

                Type expectedType = info.Members[i].NameKey switch
                {
                    nameof(SimpleClass.Itm1) => typeof(bool),
                    nameof(SimpleClass.Itm2) => typeof(int),
                    nameof(SimpleClass.Itm3) => typeof(string),
                    _ => throw new Exception("Incorrect key")
                };

                Assert.AreEqual(expectedType, info.Members[i].MemberType);
            };

            GenObjectReflector.Release(ref info);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Class_Properties(bool isValueTypeParent)
        {
            Setup(false);

            var info = new ObjectReflectorInfo();            

            GenObjectReflector.GetAllMembersInfo(ref info, isValueTypeParent ? typeof(PropertyStruct) : typeof(PropertyClass), Generator);

            Assert.AreEqual(2, info.UnmappedMembers);
            Assert.AreEqual(2, info.Members.Length);

            for (int i = 0; i < 2; i++)
            {
                Assert.AreEqual(null, info.Members[i].ExistingMap);
                Assert.AreEqual(null, info.Members[i].Accessor);
                Assert.IsInstanceOfType(info.Members[i].Info, typeof(PropertyInfo));

                Type expectedType = info.Members[i].NameKey switch
                {
                    nameof(PropertyClass.A) => typeof(string),
                    nameof(PropertyClass.B) => typeof(bool),
                    _ => throw new Exception("Invalid key")
                };

                Assert.AreEqual(expectedType, info.Members[i].MemberType);
            };

            GenObjectReflector.Release(ref info);
        }
    }
}
