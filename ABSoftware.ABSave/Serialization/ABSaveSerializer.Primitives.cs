using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ABSoftware.ABSave.Serialization
{
    public sealed partial class ABSaveSerializer
    {
        #region Byte Writing

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByte(byte byt) => Output.WriteByte(byt);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByteArray(byte[] arr) => Output.Write(arr, 0, arr.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBytes(ReadOnlySpan<byte> data) => Output.Write(data);

        #endregion

        #region Numerical Writing

        public unsafe void WriteInt16(short num)
        {
            byte* data = (byte*)&num;
            if (ShouldReverseEndian)
            {
                Output.WriteByte(data[1]);
                Output.WriteByte(data[0]);
            }
            else
            {
                Output.WriteByte(data[0]);
                Output.WriteByte(data[1]);
            }
        }

        public unsafe void WriteInt32(int num) => NumericalWriteBytes((byte*)&num, 4);
        public unsafe void WriteInt64(long num) => NumericalWriteBytes((byte*)&num, 8);
        public unsafe void WriteSingle(float num) => NumericalWriteBytes((byte*)&num, 4);
        public unsafe void WriteDouble(double num) => NumericalWriteBytes((byte*)&num, 8);

        public void WriteDecimal(decimal num)
        {
            var bits = decimal.GetBits(num);
            for (int i = 0; i < 4; i++)
                WriteInt32(bits[i]);
        }

        unsafe void NumericalWriteBytes(byte* data, int numberOfBytes)
        {
            if (ShouldReverseEndian)
            {
                byte* buffer = stackalloc byte[numberOfBytes];
                byte* bufferPos = buffer;

                byte* currentDataPos = data + numberOfBytes;
                for (int i = 0; i < numberOfBytes; i++)
                    *bufferPos++ = *--currentDataPos;

                Output.Write(new ReadOnlySpan<byte>(buffer, numberOfBytes));
            }

            else Output.Write(new ReadOnlySpan<byte>(data, numberOfBytes));
        }

        #endregion
    }
}
