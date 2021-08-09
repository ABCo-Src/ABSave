using ABCo.ABSave.Helpers;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ABCo.ABSave.Serialization
{
    public sealed partial class ABSaveSerializer
    {
        public void WriteNullableString(string? str)
        {
            using var writer = GetHeader();
            writer.WriteNullableString(str);
        }
        public void WriteNonNullString(string str)
        {
            using var writer = GetHeader();
            writer.WriteNonNullString(str);
        }
    }
}
