using ABCo.ABSave.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Serialization.Reading.Core
{
    internal static class HeaderDeserializer
    {
        public static void ReadHeader(BitReader header)
        {
            if (!header.State.Settings.IncludeVersioningHeader) return;
            header.State.HasVersioningInfo = header.ReadBit();
        }
    }
}
