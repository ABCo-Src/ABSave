using ABCo.ABSave.Helpers;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ABCo.ABSave.Serialization.Writing.Core
{
    internal static class TextSerializer
    {
        public static void WriteString(string? str, ABSaveSerializer header)
        {
            if (str == null) header.WriteBitOff();
            else
            {
                header.WriteBitOn();
                WriteNonNullString(str, header);
            }
        }

        public static void WriteNonNullString(string str, ABSaveSerializer header) => WriteText(str.AsSpan(), header);

        public static void WriteText(ReadOnlySpan<char> chars, ABSaveSerializer header)
        {
            if (header.State.Settings.UseUTF8)
                WriteUTF8(chars, header);
            else
            {
                header.WriteCompressedInt((uint)chars.Length);
                header.FastWriteShorts(MemoryMarshal.Cast<char, short>(chars));
            }
        }

        public static void WriteUTF8(ReadOnlySpan<char> data, ABSaveSerializer header)
        {
            int maxSize = Encoding.UTF8.GetMaxByteCount(data.Length);
            Span<byte> buffer = maxSize <= ABSaveUtils.MAX_STACK_SIZE ? stackalloc byte[maxSize] : header.State.GetStringBuffer(maxSize);

            int actualSize = Encoding.UTF8.GetBytes(data, buffer);

            header.WriteCompressedInt((uint)actualSize);
            header.WriteRawBytes(buffer.Slice(0, actualSize));
        }
    }
}
