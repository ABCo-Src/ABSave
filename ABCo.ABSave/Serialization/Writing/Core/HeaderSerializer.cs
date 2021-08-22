using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Serialization.Writing.Core
{
    internal static class HeaderSerializer
    {
        public static void WriteHeader(ABSaveSerializer header)
        {
            if (!header.State.Settings.IncludeVersioningHeader) return;
            header.WriteBitWith(header.State.HasVersioningInfo);
        }
    }
}
