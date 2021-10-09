using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Mapping
{
    /// <summary>
    /// Info about a member that's shared across all versions it occurs.
    /// </summary>
    internal class ObjectMemberSharedInfo
    {
        public MapItemInfo Map;
        public MemberAccessor Accessor;
    }

    // "ObjectMemberSharedInfo" but with the order of each property attached. This class is used for all members when one of the members has multiple orders on it.
    // Attaching the order to each member allows it to quickly binary search and emplace the multiple orders member in in the right place for a given version.
    internal class ObjectMemberSharedInfoWithOrder : ObjectMemberSharedInfo, IComparable<ObjectMemberSharedInfoWithOrder>
    {
        public int Order;

        public ObjectMemberSharedInfoWithOrder(int order) => Order = order;

        public int CompareTo(ObjectMemberSharedInfoWithOrder? other) => Order.CompareTo(other!.Order);
    }
}
