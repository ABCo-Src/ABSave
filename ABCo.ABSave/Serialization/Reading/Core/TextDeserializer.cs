using ABCo.ABSave.Helpers;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ABCo.ABSave.Serialization.Reading.Core
{
    internal unsafe static class TextDeserializer
    {
        public static string? ReadNullableString(ABSaveDeserializer deserializer)
        {
            if (!deserializer.ReadBit()) return null;

            return ReadNonNullString(deserializer);
        }

        public static string ReadNonNullString(ABSaveDeserializer deserializer)
        {
            if (deserializer.State.Settings.UseUTF8)
            {
                int byteSize = (int)deserializer.ReadCompressedInt();

                // Read the data
                Span<byte> buffer = byteSize <= ABSaveUtils.MAX_STACK_SIZE ? stackalloc byte[byteSize] : deserializer.State.GetStringBuffer(byteSize);
                deserializer.ReadBytes(buffer);

                // Encode
#if NETSTANDARD2_0
                fixed (byte* bufferPtr = buffer)
                    return Encoding.UTF8.GetString(bufferPtr, buffer.Length);
#else
                return Encoding.UTF8.GetString(buffer);
#endif
            }
            else
            {
                int size = (int)deserializer.ReadCompressedInt();
                int byteSize = size * sizeof(char);

#if NETSTANDARD2_0
                Span<byte> buffer = size <= ABSaveUtils.MAX_STACK_SIZE ? stackalloc byte[byteSize] : deserializer.State.GetStringBuffer(byteSize);
                deserializer.FastReadShorts(MemoryMarshal.Cast<byte, short>(buffer));
                return buffer.ToString();
#else
                return string.Create(size, deserializer, (chars, state) =>
                {
                    state.FastReadShorts(MemoryMarshal.Cast<char, short>(chars));
                });
#endif
            }
        }

        public static T ReadUTF8<T>(Func<int, T> createDest, Func<T, Memory<char>> castDest, ABSaveDeserializer header)
        {
            int byteSize = (int)header.ReadCompressedInt();

            // Read the data
            Span<byte> buffer = byteSize <= ABSaveUtils.MAX_STACK_SIZE ? stackalloc byte[byteSize] : header.State.GetStringBuffer(byteSize);
            header.ReadBytes(buffer);

            // Allocate the destination with the correct size.

#if NETSTANDARD2_0
            int charSize;
            fixed (byte* bufferPtr = buffer)
                charSize = Encoding.UTF8.GetCharCount(bufferPtr, buffer.Length);
#else
            int charSize = Encoding.UTF8.GetCharCount(buffer);
#endif

            T dest = createDest(charSize);
            Memory<char> destMem = castDest(dest);

            // Encode
#if NETSTANDARD2_0
            fixed (byte* bufferPtr = buffer)
                using (var destMemPtr = destMem.Pin())
                    Encoding.UTF8.GetChars(bufferPtr, byteSize, (char*)destMemPtr.Pointer, destMem.Length);
#else
            Encoding.UTF8.GetChars(buffer.Slice(byteSize), destMem.Span);
#endif
            return dest;
        }
    }
}
