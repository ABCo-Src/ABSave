using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace ABCo.ABSave.Serialization
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
            if (ShouldReverseEndian)
            {
                num = BinaryPrimitives.ReverseEndianness(num);
            }

            WriteBytes(new ReadOnlySpan<byte>((byte*)&num, 2));
        }

        public unsafe void WriteInt32(int num)
        {
            if (ShouldReverseEndian)
            {
                num = BinaryPrimitives.ReverseEndianness(num);
            }

            WriteBytes(new ReadOnlySpan<byte>((byte*)&num, 4));
        }

        public unsafe void WriteInt64(long num)
        {
            if (ShouldReverseEndian)
            {
                num = BinaryPrimitives.ReverseEndianness(num);
            }

            WriteBytes(new ReadOnlySpan<byte>((byte*)&num, 8));
        }

        public unsafe void WriteSingle(float num)
        {
            if (ShouldReverseEndian)
            {
                int asInt = BitConverter.SingleToInt32Bits(num);
                asInt = BinaryPrimitives.ReverseEndianness(asInt);
                WriteBytes(new ReadOnlySpan<byte>((byte*)&asInt, 4));
            }
            else
            {
                WriteBytes(new ReadOnlySpan<byte>((byte*)&num, 4));
            }
        }

        public unsafe void WriteDouble(double num)
        {
            if (ShouldReverseEndian)
            {
                long asInt = BitConverter.DoubleToInt64Bits(num);
                asInt = BinaryPrimitives.ReverseEndianness(asInt);
                WriteBytes(new ReadOnlySpan<byte>((byte*)&asInt, 8));
            }
            else
            {
                WriteBytes(new ReadOnlySpan<byte>((byte*)&num, 8));
            }
        }

        public void WriteDecimal(decimal num)
        {
            var bits = decimal.GetBits(num);
            for (int i = 0; i < 4; i++)
            {
                WriteInt32(bits[i]);
            }
        }

        #endregion
    }
}
