using ABCo.ABSave.Helpers;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ABCo.ABSave.Deserialization
{
    public sealed partial class ABSaveDeserializer
    {
        #region Text Reading

        public string? ReadNullableString() => TextDeserializer.ReadNullableString(_currentBitReader);

        public string ReadNonNullString() => TextDeserializer.ReadNonNullString(_currentBitReader);

        public unsafe void FastReadShorts(Span<short> dest)
        {
            Span<byte> destBytes = MemoryMarshal.Cast<short, byte>(dest);

            if (State.ShouldReverseEndian)
            {
                // TODO: Optimize?
                byte* buffer = stackalloc byte[2];
                var bufferSpan = new Span<byte>(buffer, 2);

                int i = 0;
                while (i < destBytes.Length)
                {
                    ReadBytes(bufferSpan);

                    destBytes[i++] = buffer[1];
                    destBytes[i++] = buffer[0];
                }
            }
            else ReadBytes(destBytes);
        }

        #endregion
    }
}