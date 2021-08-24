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
                return Encoding.UTF8.GetString(buffer);
            }
            else
            {
                int size = (int)deserializer.ReadCompressedInt();

                return string.Create(size, deserializer, (chars, state) =>
                {
                    state.FastReadShorts(MemoryMarshal.Cast<char, short>(chars));
                });
            }
        }

        public static T ReadUTF8<T>(Func<int, T> createDest, Func<T, Memory<char>> castDest, ABSaveDeserializer header)
        {
            int byteSize = (int)header.ReadCompressedInt();

            // Read the data
            Span<byte> buffer = byteSize <= ABSaveUtils.MAX_STACK_SIZE ? stackalloc byte[byteSize] : header.State.GetStringBuffer(byteSize);
            header.ReadBytes(buffer);

            // Allocate the destination with the correct size.
            int charSize = Encoding.UTF8.GetCharCount(buffer);
            T dest = createDest(byteSize);
            Memory<char> destMem = castDest(dest);

            // Encode
            Encoding.UTF8.GetChars(buffer, destMem.Span);
            return dest;
        }
    }
}
