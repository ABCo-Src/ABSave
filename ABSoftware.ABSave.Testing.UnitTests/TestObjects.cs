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
        internal bool Itm1;
        public int Itm2;
        public string Itm3;

        public SimpleClass() { }
        public SimpleClass(bool itm1, int itm2, string itm3)
        {
            Itm1 = itm1;
            Itm2 = itm2;
            Itm3 = itm3;
        }

        public bool IsEquivalentTo(SimpleClass other) => Itm1 == other.Itm1 && Itm2 == other.Itm2 && Itm3 == other.Itm3;
    }
}
