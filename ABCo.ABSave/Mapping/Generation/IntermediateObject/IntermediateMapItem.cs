using ABCo.ABSave.Mapping.Description.Attributes;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ABCo.ABSave.Mapping.Generation.IntermediateObject
{
    internal sealed class IntermediateItem : IComparable<IntermediateItem>
    {
        public int Order;

        // Whether this item has been processed into an "ObjectMemberSharedInfo" yet or not.
        public bool IsProcessed;

        public volatile bool AttributesProcessed;
        public bool DoNotSave;

        public uint StartVer;
        public uint EndVer;

        public Info Details;

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
        public int CompareTo(IntermediateItem? other) => Order.CompareTo(other!.Order);
    }
}
