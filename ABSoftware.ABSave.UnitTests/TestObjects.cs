using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.UnitTests
{
    public struct ValueTypeObj { }
    public class ReferenceTypeSub : ReferenceTypeBase { }
    public class ReferenceTypeBase { }

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

    public struct SimpleStruct
    {
        public bool Itm1;
        public int Itm2;
        public string Itm3;

        public SimpleStruct(bool itm1, int itm2, string itm3) =>
            (Itm1, Itm2, Itm3) = (itm1, itm2, itm3);
    }

    public class PropertyClass
    {
        public string A { get; set; }
        public bool B { get; set; }
    }

    public struct PropertyStruct
    {
        public string A { get; set; }
        public bool B { get; set; }
    }

    public class ClassWithUnspportedValueType
    {
        public SimpleStruct S { get; set; }
    }
}
