using ABCo.ABSave.Helpers;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ABCo.ABSave.Serialization
{
    internal static class TextSerializer
    {
        public static void WriteString(string? str, BitWriter header)
        {
            if (str == null) header.WriteBitOff();
            else
            {
                header.WriteBitOn();
                WriteNonNullString(str, header);
            }
        }

        public static void WriteNonNullString(string str, BitWriter header) => WriteText(str.AsSpan(), header);

        public static void WriteText(ReadOnlySpan<char> chars, BitWriter header)
        {
            if (header.State.Settings.UseUTF8)
                WriteUTF8(chars, header);
            else
            {
                header.WriteCompressedInt((uint)chars.Length);
                FastWriteShorts(MemoryMarshal.Cast<char, short>(chars), header.Finish());
            }
        }

        public static void WriteUTF8(ReadOnlySpan<char> data, BitWriter header)
        {
            int maxSize = Encoding.UTF8.GetMaxByteCount(data.Length);
            Span<byte> buffer = maxSize <= ABSaveUtils.MAX_STACK_SIZE ? stackalloc byte[maxSize] : header.State.GetStringBuffer(maxSize);

            int actualSize = Encoding.UTF8.GetBytes(data, buffer);

            header.WriteCompressedInt((uint)actualSize);

            var serializer = header.Finish();
            serializer.WriteBytes(buffer.Slice(0, actualSize));
        }

        static unsafe void FastWriteShorts(ReadOnlySpan<short> shorts, ABSaveSerializer serializer)
        {
            ReadOnlySpan<byte> bytes = MemoryMarshal.Cast<short, byte>(shorts);

            if (serializer.State.ShouldReverseEndian)
            {
                // TODO: Optimize?
                byte* buffer = stackalloc byte[2];
                var bufferSpan = new ReadOnlySpan<byte>(buffer, 2);

                int i = 0;
                while (i < bytes.Length)
                {
                    buffer[1] = bytes[i++];
                    buffer[0] = bytes[i++];

                    serializer.WriteBytes(bufferSpan);
                }
            }
            else serializer.WriteBytes(bytes);
        }
    }
}
