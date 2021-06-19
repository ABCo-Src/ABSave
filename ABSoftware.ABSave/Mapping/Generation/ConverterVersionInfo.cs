using ABCo.ABSave.Converters;
using ABCo.ABSave.Mapping.Description.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Mapping.Generation
{
    public class ConverterVersionInfo
    {
        public uint VersionNumber { get; private set; }
        public bool UsesHeader { get; private set; }

        internal SaveInheritanceAttribute? _inheritanceInfo;

        public ConverterVersionInfo(bool usesHeader) => 
            UsesHeader = usesHeader;

        internal void Initialize(uint version, bool usesHeader, Converter converter)
        {
            VersionNumber = version;
            UsesHeader = usesHeader;
            _inheritanceInfo = MapGenerator.GetConverterInheritanceInfoForVersion(version, converter);
        }
    }
}
