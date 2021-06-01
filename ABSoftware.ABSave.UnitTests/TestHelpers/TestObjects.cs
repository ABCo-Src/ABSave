using ABSoftware.ABSave.Mapping.Description.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABSoftware.ABSave.UnitTests.TestHelpers
{
    [SaveMembers]
    class EmptyClass { }

    [SaveMembers]
    class GenericType<TA, TB, TC> : Base { }

    [SaveMembers]
    class Base { }

    [SaveMembers]
    class SubNoConverter : Base 
    {
        [Save(0)]
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

    [SaveMembers]
    class SubWithHeader : Base
    {
        public override bool Equals(object obj) => obj is SubWithHeader;

        public override int GetHashCode() => base.GetHashCode();
    }

    [SaveMembers]
    class SubWithoutHeader : Base
    {
        public override bool Equals(object obj) => obj is SubWithoutHeader;

        public override int GetHashCode() => base.GetHashCode();
    }

    [SaveMembers]
    struct MyStruct
    {
        [Save(0)]
        public byte A { get; set; }

        [Save(1)]
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

    [SaveMembers]
    class GeneralClass : Base
    {
        [Save(0)]
        public byte A { get; set; }

        [Save(1)]
        public SubWithHeader B { get; set; }

        [Save(2)]
        public SubWithoutHeader C { get; set; }

        [Save(3)]
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

    [SaveMembers]
    public struct ValueTypeObj { }

    [SaveMembers]
    public class ReferenceTypeSub : ReferenceTypeBase { }

    [SaveMembers]
    public class ReferenceTypeBase { }

    [SaveMembers(ABSave.Mapping.Description.SaveMembersMode.Fields)]
    public class SimpleClass
    {
        [Save(0)]
        internal bool Itm1;

        [Save(1)]
        public int Itm2;

        [Save(2)]
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

    [SaveMembers(ABSave.Mapping.Description.SaveMembersMode.Fields)]
    public struct SimpleStruct
    {
        [Save(0)]
        public bool Itm1;

        [Save(1)]
        public int Itm2;

        [Save(2)]
        public string Itm3;

        public SimpleStruct(bool itm1, int itm2, string itm3) =>
            (Itm1, Itm2, Itm3) = (itm1, itm2, itm3);
    }

    [SaveMembers]
    public class PropertyClass
    {
        [Save(0)]
        public string A { get; set; }

        [Save(1)]
        public bool B { get; set; }
    }

    [SaveMembers]
    public struct PropertyStruct
    {
        [Save(0)]
        public string A { get; set; }

        [Save(1)]
        public bool B { get; set; }
    }

    [SaveMembers]
    public class UnorderedPropertyClass
    {
        [Save(1)]
        public string A { get; set; }

        [Save(0)]
        public bool B { get; set; }
    }

    [SaveMembers]
    public class ClassWithSkippableItem
    {
        [Save(0)]
        public string A { get; set; }

        // No attribute
        public bool Skippable { get; set; }

        [Save(2)]
        public int C { get; set; }
    }

    [SaveMembers]
    public class VersionedPropertyClass
    {
        // Version 0: A
        // Version 1: A, B, C
        // Version 2: A, C, D
        [Save(0)]
        public DateTime A { get; set; } = new DateTime(3);

        [Save(1, FromVer = 1, ToVer = 1)]
        public bool B { get; set; } = true;

        [Save(2, FromVer = 1)]
        public int C { get; set; } = 5;

        [Save(3, FromVer = 2)]
        public long D { get; set; } = 7;

        public override bool Equals(object obj)
        {
            if (obj is VersionedPropertyClass cl)
            {
                return A == cl.A && B == cl.B && C == cl.C && D == cl.D;
            }

            return false;
        }

        public override int GetHashCode() => base.GetHashCode();
    }

    [SaveMembers]
    public class ClassWithUnspportedForFastAccessorValueType
    {
        [Save(0)]
        public SimpleStruct S { get; set; }
    }

    public class UnserializableClass { }
}
