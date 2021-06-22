using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping.Description;
using ABCo.ABSave.Mapping.Description.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ABCo.ABSave.Mapping.Generation
{
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
}
