using ABSoftware.ABSave.Helpers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ABSoftware.ABSave.Mapping.Generation
{
    struct IntermediateObjInfo
    {
        public int HighestVersion;
        public ObjectIntermediateItem[] RawMembers;

        public IntermediateObjInfo(int highestVersion, ObjectIntermediateItem[] rawMembers) =>
            (HighestVersion, RawMembers) = (highestVersion, rawMembers);
    }

    internal class ObjectIntermediateItem : IComparable<ObjectIntermediateItem>
    {
        public int Order;

        // Whether this item has been processed yet or not.
        public bool IsProcessed;

        public uint StartVer;
        public uint EndVer;

        public Info Details;

        [StructLayout(LayoutKind.Explicit)]
        public struct Info
        {
            [FieldOffset(0)]
            public ObjectMemberSharedInfo Processed;

            [FieldOffset(0)]
            public MemberInfo Unprocessed;
        }

        // For sorting:
        public int CompareTo(ObjectIntermediateItem? other) => Order.CompareTo(other!.Order);
    }

    internal struct ObjectTranslatedSortInfo : IComparable<ObjectTranslatedSortInfo>
    {
        public int Order;
        public short Index;

        public int CompareTo(ObjectTranslatedSortInfo other) => Order.CompareTo(other.Order);
    }
}
