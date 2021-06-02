using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping.Description.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ABSoftware.ABSave.Mapping.Generation
{
    public struct IntermediateObjInfo
    {
        public uint HighestVersion;
        internal ObjectIntermediateItem[] RawMembers;

        internal IntermediateObjInfo(uint highestVersion, ObjectIntermediateItem[] rawMembers) =>
            (HighestVersion, RawMembers) = (highestVersion, rawMembers);
    }

    internal sealed class ObjectIntermediateItem : IComparable<ObjectIntermediateItem>
    {
        public static readonly ObjectIntermediateItem InvalidMember = new ObjectIntermediateItem();

        public int Order;

        // Whether this item has been processed into an "ObjectMemberSharedInfo" yet or not.
        public bool IsProcessed;

        public volatile bool AttributesProcessed;
        public bool DoNotSave;

        public uint StartVer;
        public uint EndVer;

        public Info Details;

        // Holds the attributes retrieved by the thread pool.
        public MapAttr[]? Attributes;

        [StructLayout(LayoutKind.Explicit)]
        public struct Info
        {
            // When the item has already been processed.
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
