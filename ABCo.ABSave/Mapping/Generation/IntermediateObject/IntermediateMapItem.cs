using ABCo.ABSave.Mapping.Description.Attributes;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ABCo.ABSave.Mapping.Generation.IntermediateObject
{
    internal class IntermediateItem : IComparable<IntermediateItem>
    {
        public int SingleOrder;

        // Whether this item has been processed into an "ObjectMemberSharedInfo" yet or not.
        public bool IsProcessed;
        public bool HasMultipleOrders;

        public uint FromVer;
        public uint ToVer;

        public Info Details;

        [StructLayout(LayoutKind.Explicit)]
        public struct Info
        {
            // When the item has already been processed, the "ObjectMemberSharedInfo" is cached here. This does NOT happen to items with multiple "Save" attributes, they'll remain forever "Unprocessed" here.
            [FieldOffset(0)]
            public ObjectMemberSharedInfo Processed;

            [FieldOffset(0)]
            public MemberInfo Unprocessed;
        }

        // For sorting:
        public int CompareTo(IntermediateItem? other) => SingleOrder.CompareTo(other!.SingleOrder);
    }

    internal sealed class IntermediateItemWithMultipleOrders : IntermediateItem
    {
        public SaveAttribute[] AllOrders;

        public IntermediateItemWithMultipleOrders(SaveAttribute[] allOrders) => (AllOrders, HasMultipleOrders, SingleOrder) = (allOrders, true, int.MaxValue);
    }
}
