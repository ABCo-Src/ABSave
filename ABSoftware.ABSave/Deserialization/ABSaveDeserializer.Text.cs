using ABCo.ABSave.Helpers;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ABCo.ABSave.Deserialization
{
    public sealed partial class ABSaveDeserializer
    {
        #region Text Reading

        public string ReadString()
        {
            var header = new BitSource(this);
            return ReadString(ref header);
        }

        public string ReadString(ref BitSource header)
        {
            if (Settings.UseUTF8)
            {
                int byteSize = (int)ReadCompressedInt(ref header);

                // Read the data
                Span<byte> buffer = byteSize <= ABSaveUtils.MAX_STACK_SIZE ? stackalloc byte[byteSize] : GetStringBuffer(byteSize);
                ReadBytes(buffer);

                // Encode
                return Encoding.UTF8.GetString(buffer);
            }
            else
            {
                int size = (int)header.Deserializer.ReadCompressedInt(ref header);
                return string.Create<object?>(size, null, (chars, state) =>
                {
                    FastReadShorts(MemoryMarshal.Cast<char, short>(chars));
                });
            }
        }

        public T ReadUTF8<T>(Func<int, T> createDest, Func<T, Memory<char>> castDest, ref BitSource header)
        {
            int byteSize = (int)ReadCompressedInt(ref header);

            // Read the data
            Span<byte> buffer = byteSize <= ABSaveUtils.MAX_STACK_SIZE ? stackalloc byte[byteSize] : GetStringBuffer(byteSize);
            ReadBytes(buffer);

            // Allocate the destination with the correct size.
            int charSize = Encoding.UTF8.GetCharCount(buffer);
            T? dest = createDest(byteSize);
            Memory<char> destMem = castDest(dest);

            // Encode
            Encoding.UTF8.GetChars(buffer, destMem.Span);
            return dest;
        }

        public unsafe void FastReadShorts(Span<short> dest)
        {
            Span<byte> destBytes = MemoryMarshal.Cast<short, byte>(dest);

            if (ShouldReverseEndian)
            {
                // TODO: Optimize?
                byte* buffer = stackalloc byte[2];
                var bufferSpan = new Span<byte>(buffer, 2);

                int i = 0;
                while (i < destBytes.Length)
                {
                    Source.Read(bufferSpan);

                    destBytes[i++] = buffer[1];
                    destBytes[i++] = buffer[0];
                }
            }
            else Source.Read(destBytes);
        }

        public byte[] GetStringBuffer(int length)
        {
            if (_stringBuffer == null || _stringBuffer.Length < length)
                return _stringBuffer = ABSaveUtils.CreateUninitializedArray<byte>(length);
            else return _stringBuffer;
        }

        #endregion
    }
}