using ABCo.ABSave.Helpers;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ABCo.ABSave.Serialization
{
    public sealed partial class ABSaveSerializer
    {
        public void WriteString(string? str)
        {
            var header = new BitTarget(this);

            if (str == null) header.WriteBitOff();
            else
            {
                header.WriteBitOn();
                WriteNonNullString(str!);
            }
        }

        public void WriteNonNullString(string str)
        {
            var header = new BitTarget(this);
            WriteString(str, ref header);
        }

        public void WriteString(string str, ref BitTarget header) => WriteText(str.AsSpan(), ref header);

        public void WriteText(ReadOnlySpan<char> chars, ref BitTarget header)
        {
            if (Settings.UseUTF8)
                WriteUTF8(chars, ref header);
            else
            {
                WriteCompressedInt((uint)chars.Length);
                FastWriteShorts(MemoryMarshal.Cast<char, short>(chars));
            }
        }

        public void WriteUTF8(ReadOnlySpan<char> data, ref BitTarget header)
        {
            int maxSize = Encoding.UTF8.GetMaxByteCount(data.Length);
            Span<byte> buffer = maxSize <= ABSaveUtils.MAX_STACK_SIZE ? stackalloc byte[maxSize] : GetStringBufferFor(maxSize);

            int actualSize = Encoding.UTF8.GetBytes(data, buffer);

            WriteCompressedInt((uint)actualSize, ref header);
            WriteBytes(buffer.Slice(0, actualSize));
        }

        byte[] GetStringBufferFor(int length)
        {
            if (_stringBuffer == null || _stringBuffer.Length < length)
                return _stringBuffer = ABSaveUtils.CreateUninitializedArray<byte>(length);

            return _stringBuffer;
        }

        public unsafe void FastWriteShorts(ReadOnlySpan<short> shorts)
        {
            ReadOnlySpan<byte> bytes = MemoryMarshal.Cast<short, byte>(shorts);

            if (ShouldReverseEndian)
            {
                // TODO: Optimize?
                byte* buffer = stackalloc byte[2];
                var bufferSpan = new ReadOnlySpan<byte>(buffer, 2);

                int i = 0;
                while (i < bytes.Length)
                {
                    buffer[1] = bytes[i++];
                    buffer[0] = bytes[i++];

                    Output.Write(bufferSpan);
                }
            }
            else Output.Write(bytes);
        }
    }
}
