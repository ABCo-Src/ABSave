using ABCo.ABSave.Helpers;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ABCo.ABSave.Serialization
{
    internal static class TextSerializer
    {
        public static void WriteString(string? str, ref BitTarget header)
        {
            if (str == null) header.WriteBitOff();
            else
            {
                header.WriteBitOn();
                WriteNonNullString(str, ref header);
            }
        }

        public static void WriteNonNullString(string str, ref BitTarget header) => WriteText(str.AsSpan(), ref header);

        public static void WriteText(ReadOnlySpan<char> chars, ref BitTarget header)
        {
            if (header.State.Settings.UseUTF8)
                WriteUTF8(chars, ref header);
            else
            {
                header.Serializer.WriteCompressedInt((uint)chars.Length, ref header);
                FastWriteShorts(MemoryMarshal.Cast<char, short>(chars), ref header);
            }
        }

        public static void WriteUTF8(ReadOnlySpan<char> data, ref BitTarget header)
        {
            int maxSize = Encoding.UTF8.GetMaxByteCount(data.Length);
            Span<byte> buffer = maxSize <= ABSaveUtils.MAX_STACK_SIZE ? stackalloc byte[maxSize] : header.State.GetStringBuffer(maxSize);

            int actualSize = Encoding.UTF8.GetBytes(data, buffer);

            header.Serializer.WriteCompressedInt((uint)actualSize, ref header);
            header.Serializer.WriteBytes(buffer.Slice(0, actualSize));
        }

        static unsafe void FastWriteShorts(ReadOnlySpan<short> shorts, ref BitTarget header)
        {
            ReadOnlySpan<byte> bytes = MemoryMarshal.Cast<short, byte>(shorts);

            if (header.Serializer.State.ShouldReverseEndian)
            {
                // TODO: Optimize?
                byte* buffer = stackalloc byte[2];
                var bufferSpan = new ReadOnlySpan<byte>(buffer, 2);

                int i = 0;
                while (i < bytes.Length)
                {
                    buffer[1] = bytes[i++];
                    buffer[0] = bytes[i++];

                    header.Serializer.WriteBytes(bufferSpan);
                }
            }
            else header.Serializer.WriteBytes(bytes);
        }
    }
}
