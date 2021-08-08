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
            var header = new BitTarget(this);
            TextSerializer.WriteString(str, ref header);
        }

        public void WriteNullableString(string? str, ref BitTarget header) => TextSerializer.WriteString(str, ref header);

        public void WriteNonNullString(string str)
        {
            var header = new BitTarget(this);
            TextSerializer.WriteNonNullString(str, ref header);
        }

        public void WriteNonNullString(string str, ref BitTarget header) => TextSerializer.WriteNonNullString(str, ref header);

        public void WriteText(ReadOnlySpan<char> bytes, ref BitTarget header) => TextSerializer.WriteText(bytes, ref header);

        public void WriteUTF8(ReadOnlySpan<char> bytes, ref BitTarget header) => TextSerializer.WriteUTF8(bytes, ref header);
    }
}
