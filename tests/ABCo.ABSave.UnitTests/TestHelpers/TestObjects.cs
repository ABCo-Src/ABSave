using ABCo.ABSave.Mapping.Description;
using ABCo.ABSave.Mapping.Description.Attributes;
using ABCo.ABSave.TestOtherAssembly;
using System;

namespace ABCo.ABSave.UnitTests.TestHelpers
{
    [SaveMembers]
    class EmptyClass { }

    public struct ConverterValueType { }

    [SaveMembers]
    class GenericType<TA, TB, TC> : BaseIndex { }

    #region Index Inheritance

    [SaveMembers]
    [SaveInheritance(SaveInheritanceMode.Index, ToVer = 1)]
    class ClassWithMinVersion { }

    [SaveInheritance(SaveInheritanceMode.Index, typeof(SubEmpty), typeof(SubNoConverter), typeof(SubWithHeader), typeof(SubWithoutHeader))]
    class BaseIndex { }

    [SaveMembers]
    class SubEmpty : BaseIndex { }

    [SaveMembers]
    class SubNoConverter : BaseIndex
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

    class SubWithHeader : BaseIndex
    {
        public override bool Equals(object obj) => obj is SubWithHeader;

        public override int GetHashCode() => base.GetHashCode();
    }

    class SubWithoutHeader : BaseIndex
    {
        public override bool Equals(object obj) => obj is SubWithoutHeader;

        public override int GetHashCode() => base.GetHashCode();
    }

    #endregion

    #region General

    [SaveMembers]
    class NestedClass
    {
        [Save(0)]
        public byte A { get; set; }

        [Save(1)]
        public SubWithHeader B { get; set; }

        [Save(2)]
        public SubWithoutHeader C { get; set; }

        [Save(3)]
        public VerySimpleStruct D { get; set; }

        public NestedClass() { }
        public NestedClass(byte a)
        {
            A = a;
            B = new SubWithHeader();
            C = new SubWithoutHeader();
            D = new VerySimpleStruct(a, 9);
        }

        public override bool Equals(object obj)
        {
            if (obj is NestedClass right)
            {
                return A == right.A && B.Equals(right.B) && C.Equals(right.C) && D.Equals(right.D);
            }

            return false;
        }

        public override int GetHashCode() => base.GetHashCode();
    }

    [SaveMembers]
    public class AllPrimitiveClass
    {
        [Save(0)]
        internal bool Itm1 { get; set; }

        [Save(1)]
        public int Itm2 { get; set; }

        [Save(2)]
        public string Itm3 { get; set; }

        public AllPrimitiveClass() { }
        public AllPrimitiveClass(bool itm1, int itm2, string itm3)
        {
            Itm1 = itm1;
            Itm2 = itm2;
            Itm3 = itm3;
        }

        public bool IsEquivalentTo(AllPrimitiveClass other) => Itm1 == other.Itm1 && Itm2 == other.Itm2 && Itm3 == other.Itm3;
    }

    [SaveMembers]
    public struct AllPrimitiveStruct
    {
        [Save(0)]
        public bool A { get; set; }

        [Save(1)]
        public int B { get; set; }

        [Save(2)]
        public string C { get; set; }

        public AllPrimitiveStruct(bool itm1, int itm2, string itm3) =>
            (A, B, C) = (itm1, itm2, itm3);
    }

    [SaveMembers]
    struct VerySimpleStruct
    {
        [Save(0)]
        public byte A { get; set; }

        [Save(1)]
        public byte B { get; set; }

        public VerySimpleStruct(byte a, byte b)
        {
            A = a;
            B = b;
        }

        public override bool Equals(object obj)
        {
            if (obj is VerySimpleStruct right)
            {
                return A == right.A && B == right.B;
            }

            return false;
        }

        public override string ToString() => $"{A}:{B}";

        public override int GetHashCode() => base.GetHashCode();
    }

    [SaveMembers]
    class SingleMemberClass
    {
        [Save(0)]
        public byte A { get; set; }
    }

    [SaveMembers]
    [SaveBaseMembers(typeof(SingleMemberClass), FromVer = 1)]
    class MemberInheritingSingleClass : SingleMemberClass
    {
        [Save(0)]
        public byte B { get; set; }
    }

    [SaveMembers]
    [SaveBaseMembers(typeof(SingleMemberClass), FromVer = 0)]
    [SaveBaseMembers(typeof(MemberInheritingSingleClass), FromVer = 1)]
    class MemberInheritingDoubleClass : MemberInheritingSingleClass
    {
        [Save(0)]
        public byte C { get; set; }

        public MemberInheritingDoubleClass() { }
        public MemberInheritingDoubleClass(byte a, byte b, byte c) => (A, B, C) = (a, b, c);

        public override bool Equals(object obj) => obj is MemberInheritingDoubleClass cl && A == cl.A && B == cl.B && C == cl.C;
        public override int GetHashCode() => B;
    }

    #endregion

    #region Mapping Classes

    [SaveMembers(SaveMembersMode.Fields)]
    public class FieldClass
    {
        [Save(0)]
        public string A;

        [Save(1)]
        public bool B;
    }

    [SaveMembers(SaveMembersMode.Fields)]
    public struct FieldStruct
    {
        [Save(0)]
        public string A;

        [Save(1)]
        public bool B;
    }

    [SaveMembers]
    public class UnorderedClass
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
    public class ClassWithUnspportedForFastAccessorValueType
    {
        [Save(0)]
        public AllPrimitiveStruct S { get; set; }
    }

    [SaveMembers]
    [SaveInheritance(SaveInheritanceMode.Key, FromVer = 0, ToVer = 2)]
    [SaveInheritance(SaveInheritanceMode.IndexOrKey, FromVer = 3)]
    public class VersionedClass
    {
        // Version 0: A
        // Version 1: A, B, C
        // Version 2: A, C, D
        [Save(0)]
        public DateTime A { get; set; } = new DateTime(3);

        [Save(1, FromVer = 1, ToVer = 2)]
        public bool B { get; set; } = true;

        [Save(2, FromVer = 1)]
        public int C { get; set; } = 5;

        [Save(3, FromVer = 2)]
        public long D { get; set; } = 7;

        public override bool Equals(object obj)
        {
            if (obj is VersionedClass cl)
            {
                return A == cl.A && B == cl.B && C == cl.C && D == cl.D;
            }

            return false;
        }

        public override int GetHashCode() => base.GetHashCode();
    }

    [SaveMembers]
    class InvalidSaveAttributeClass
    {
        [Save(3, FromVer = 9, ToVer = 9)]
        public int A { get; set; }
    }

    public class UnserializableClass { }

    #endregion

    #region Key Inheritance Modes

    [SaveMembers]
    [SaveInheritance(SaveInheritanceMode.Key)]
    public class KeyBase { }

    [SaveInheritanceKey("First")]
    public class KeySubFirst : KeyBase { }

    [SaveInheritanceKey("Second")]
    public class KeySubSecond : KeyBase { }

    [SaveMembers]
    [SaveInheritance(SaveInheritanceMode.IndexOrKey, typeof(IndexKeySubIndex))]
    public class IndexKeyBase { }

    public class IndexKeySubIndex : IndexKeyBase { }

    [SaveInheritanceKey("Key")]
    public class IndexKeySubKey : IndexKeyBase { }

    // OtherAssemblyBase is in the "OtherAssembly".

    [SaveMembers]
    [SaveInheritanceKey("Second")]
    public class CrossAssemblySub : OtherAssemblyBase { }

    #endregion

}
