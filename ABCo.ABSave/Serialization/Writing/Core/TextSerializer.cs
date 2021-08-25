using ABCo.ABSave.Helpers;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ABCo.ABSave.Serialization.Writing.Core
{
    internal static class TextSerializer
    {
        public static void WriteString(string? str, ABSaveSerializer serializer)
        {
            if (str == null) serializer.WriteBitOff();
            else
            {
                serializer.WriteBitOn();
                WriteNonNullString(str, serializer);
            }
        }

        public static void WriteNonNullString(string str, ABSaveSerializer serializer) => WriteText(str.AsSpan(), serializer);

        public static void WriteText(ReadOnlySpan<char> chars, ABSaveSerializer serializer)
        {
            if (serializer.State.Settings.UseUTF8)
                WriteUTF8(chars, serializer);
            else
            {
                serializer.WriteCompressedInt((uint)chars.Length);
                serializer.FastWriteShorts(MemoryMarshal.Cast<char, short>(chars));
            }
        }

        public static unsafe void WriteUTF8(ReadOnlySpan<char> data, ABSaveSerializer serializer)
        {
            int maxSize = Encoding.UTF8.GetMaxByteCount(data.Length);
            Span<byte> buffer = maxSize <= ABSaveUtils.MAX_STACK_SIZE ? stackalloc byte[maxSize] : serializer.State.GetStringBuffer(maxSize);

#if NETSTANDARD2_0
            int actualSize;

            fixed (char* dataPtr = data)
            fixed (byte* bufferPtr = buffer)
                actualSize = Encoding.UTF8.GetBytes(dataPtr, data.Length, bufferPtr, buffer.Length);
#else
            int actualSize = Encoding.UTF8.GetBytes(data, buffer);
#endif

            serializer.WriteCompressedInt((uint)actualSize);
            serializer.WriteRawBytes(buffer.Slice(0, actualSize));
        }
    }
}
