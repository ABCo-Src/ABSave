using ABCo.ABSave.Mapping.Description.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Mapping
{
    public class VersionInfo
    {
        public uint VersionNumber { get; private set; }

        internal SaveInheritanceAttribute? _inheritanceInfo;

        protected internal VersionInfo() { }

        internal void Assign(uint version, SaveInheritanceAttribute? inheritanceInfo)
        {
            VersionNumber = version;
            _inheritanceInfo = inheritanceInfo;
        }
    }
}
