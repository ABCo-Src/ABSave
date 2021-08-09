using ABCo.ABSave.Helpers;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ABCo.ABSave.Deserialization.Core
{
    internal unsafe static class TextDeserializer
    {
        public static string? ReadNullableString(BitReader header)
        {
            if (!header.ReadBit()) return null;

            return ReadNonNullString(header);
        }

        public static string ReadNonNullString(BitReader header)
        {
            if (header.State.Settings.UseUTF8)
            {
                int byteSize = (int)header.ReadCompressedInt();

                var deserializer = header.Finish();

                // Read the data
                Span<byte> buffer = byteSize <= ABSaveUtils.MAX_STACK_SIZE ? stackalloc byte[byteSize] : header.State.GetStringBuffer(byteSize);
                deserializer.ReadBytes(buffer);

                // Encode
                return Encoding.UTF8.GetString(buffer);
            }
            else
            {
                int size = (int)header.ReadCompressedInt();

                var deserializer = header.Finish();
                return string.Create(size, deserializer, (chars, state) =>
                {
                    state.FastReadShorts(MemoryMarshal.Cast<char, short>(chars));
                });
            }
        }

        public static T ReadUTF8<T>(Func<int, T> createDest, Func<T, Memory<char>> castDest, BitReader header)
        {
            int byteSize = (int)header.ReadCompressedInt();

            var deserializer = header.Finish();

            // Read the data
            Span<byte> buffer = byteSize <= ABSaveUtils.MAX_STACK_SIZE ? stackalloc byte[byteSize] : header.State.GetStringBuffer(byteSize);
            deserializer.ReadBytes(buffer);

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
