using ABCo.ABSave.Mapping;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Serialization
{
    /// <summary>
    /// Details about a converter that get cached after the first time it's used during the serialization process.
    /// The first time the converter is in a situation that requires anything in here, some data may be written/read, and that data is then left out in the future as the data is present here.
    /// </summary>
    internal struct CachedConverterDetails
    {
        /// <summary>
        /// The current version info for this converter. The first time the converter is encountered this is obtained, the version number is written, and the value is then cached in here.
        /// </summary>
        public VersionInfo CurrentInfo;
        public int? KeyInheritanceCachedValue;
    }
}
