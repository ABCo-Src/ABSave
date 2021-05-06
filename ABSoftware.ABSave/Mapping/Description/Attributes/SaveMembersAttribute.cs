using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Mapping.Description.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class SaveMembersAttribute : MapAttr
    {
        public SaveMembersMode Mode;

        public SaveMembersAttribute(SaveMembersMode mode = SaveMembersMode.Properties)
            => Mode = mode;
    }
}
