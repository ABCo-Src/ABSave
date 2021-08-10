using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Serialization.Writing.Core
{
    internal static class HeaderSerializer
    {
        public static void WriteHeader(BitWriter writer)
        {
            writer.WriteBitWith(writer.State.WriteVersioning);
        }
    }
}
