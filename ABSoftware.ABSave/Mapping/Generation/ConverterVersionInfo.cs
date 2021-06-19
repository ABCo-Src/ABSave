using ABCo.ABSave.Mapping.Description.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Mapping.Generation
{
    public class ConverterVersionInfo
    {
        public uint VersionNumber { get; }
        public bool UsesHeader { get; }

        internal SaveInheritanceAttribute? _inheritanceInfo;

        public ConverterVersionInfo(uint versionNumber, bool usesHeader, SaveInheritanceAttribute? inheritanceInfo) => 
            (VersionNumber, UsesHeader, _inheritanceInfo) = (versionNumber, usesHeader, inheritanceInfo);
    }
}
