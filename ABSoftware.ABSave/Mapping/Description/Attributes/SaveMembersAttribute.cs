﻿using System;

namespace ABCo.ABSave.Mapping.Description.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class SaveMembersAttribute : MapAttr
    {
        public SaveMembersMode Mode;

        public SaveMembersAttribute(SaveMembersMode mode = SaveMembersMode.Properties)
            => Mode = mode;
    }
}
