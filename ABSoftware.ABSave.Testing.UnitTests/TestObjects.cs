using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Testing.UnitTests
{
    public struct ValueTypeObj { }
    public class ReferenceTypeSub { }
    public class ReferenceTypeBase { }

    public class SimpleObject
    {
        internal bool Itm1 = true;
        public int Itm2 = 12;
        public string Itm3 = "abc";
    }

    public class DeepObject
    {
        internal SimpleObject Simple = new SimpleObject();
        public string NullItm = null;
    }
}
