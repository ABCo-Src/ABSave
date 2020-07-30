using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Testing.UnitTests
{
    public struct ValueTypeObj { }
    public class ReferenceTypeSub : ReferenceTypeBase { }
    public class ReferenceTypeBase { }

    public struct SimpleStruct
    {
        public int Inside;

        public SimpleStruct(int inside) => Inside = inside;
    }

    public class SimpleClass
    {
        internal bool Itm1 = true;
        public int Itm2 = 12;
        public string Itm3 = "abc";
    }

    public class DeepObject
    {
        internal SimpleClass Simple = new SimpleClass();
        public string NullItm = null;
    }
}
