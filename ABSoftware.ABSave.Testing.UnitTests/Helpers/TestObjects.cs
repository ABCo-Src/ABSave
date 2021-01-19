using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABSoftware.ABSave.Testing.UnitTests.Helpers
{
    class GenericType<TA, TB, TC> : Base { }
    class Base { }
    class SubNoConverter : Base 
    {
        public byte A { get; set; }

        public SubNoConverter() { }
        public SubNoConverter(byte a) => A = a;

        public override bool Equals(object obj)
        {
            if (obj is SubNoConverter right)
            {
                return A == right.A;
            }

            return false;
        }

        public override int GetHashCode() => base.GetHashCode();
    }

    class SubWithHeader : Base
    {
        public override bool Equals(object obj) => obj is SubWithHeader;

        public override int GetHashCode() => base.GetHashCode();
    }

    class SubWithoutHeader : Base
    {
        public override bool Equals(object obj) => obj is SubWithoutHeader;

        public override int GetHashCode() => base.GetHashCode();
    }

    struct MyStruct
    {
        public byte A { get; set; }
        public byte B { get; set; }

        public MyStruct(byte a, byte b)
        {
            A = a;
            B = b;
        }

        public override bool Equals(object obj)
        {
            if (obj is MyStruct right)
            {
                return A == right.A && B == right.B;
            }

            return false;
        }

        public override string ToString() => $"{A}:{B}";

        public override int GetHashCode() => base.GetHashCode();
    }

    class GeneralClass : Base
    {
        public byte A { get; set; }
        public SubWithHeader B { get; set; }
        public SubWithoutHeader C { get; set; }
        public MyStruct D { get; set; }

        public GeneralClass() { }
        public GeneralClass(byte a)
        {
            A = a;
            B = new SubWithHeader();
            C = new SubWithoutHeader();
            D = new MyStruct(a, 9);
        }

        public override bool Equals(object obj)
        {
            if (obj is GeneralClass right)
            {
                return A == right.A && B.Equals(right.B) && C.Equals(right.C) && D.Equals(right.D);
            }

            return false;
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
