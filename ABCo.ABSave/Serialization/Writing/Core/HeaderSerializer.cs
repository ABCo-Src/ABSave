using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Serialization.Writing.Core
{
    internal static class HeaderSerializer
    {
        public static void WriteHeader(ABSaveSerializer serializer)
        {
            if (!serializer.State.Settings.IncludeVersioningHeader) return;
            serializer.WriteBitWith(serializer.State.HasVersioningInfo);
        }
    }
}
