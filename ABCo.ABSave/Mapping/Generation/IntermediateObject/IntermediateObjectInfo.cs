using ABCo.ABSave.Mapping.Description.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Mapping.Generation.IntermediateObject
{
    internal struct IntermediateObjectInfo
    {
        // Null if intermediate info is released.
        public IntermediateItem[]? Members;
        public SaveBaseMembersAttribute[]? BaseMemberAttributes;

        public bool HasMembersWithMultipleOrders;

        public void Release()
        {
            Members = null;
            BaseMemberAttributes = null;
        }
    }
}
