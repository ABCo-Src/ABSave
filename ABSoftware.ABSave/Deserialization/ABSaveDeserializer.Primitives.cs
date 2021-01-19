using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ABSoftware.ABSave.Deserialization
{
    public sealed partial class ABSaveDeserializer
    {
        #region Byte Reading

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte() => (byte)Source.ReadByte();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadBytes(Span<byte> dest) => Source.Read(dest);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadBytes(byte[] dest) => Source.Read(dest, 0, dest.Length);

        #endregion

        #region Numerical Reading

        public unsafe short ReadInt16()
        {
            short res = 0;
            ReadBytes(new Span<byte>((byte*)&res, 2));
            return ShouldReverseEndian ? BinaryPrimitives.ReverseEndianness(res) : res;
        }

        public unsafe int ReadInt32()
        {
            int res = 0;
            ReadBytes(new Span<byte>((byte*)&res, 4));
            return ShouldReverseEndian ? BinaryPrimitives.ReverseEndianness(res) : res;
        }

        public unsafe long ReadInt64()
        {
            long res = 0;
            ReadBytes(new Span<byte>((byte*)&res, 8));
            return ShouldReverseEndian ? BinaryPrimitives.ReverseEndianness(res) : res;
        }

        public unsafe float ReadSingle()
        {
            if (ShouldReverseEndian)
            {
                int res = 0;
                ReadBytes(new Span<byte>((byte*)&res, 4));
                return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReverseEndianness(res));
            }
            else
            {
                float res = 0;
                ReadBytes(new Span<byte>((byte*)&res, 4));
                return res;
            }
        }

        public unsafe double ReadDouble()
        {
            if (ShouldReverseEndian)
            {
                long res = 0;
                ReadBytes(new Span<byte>((byte*)&res, 8));
                return BitConverter.Int64BitsToDouble(BinaryPrimitives.ReverseEndianness(res));
            }
            else
            {
                double res = 0;
                ReadBytes(new Span<byte>((byte*)&res, 8));
                return res;
            }
        }

        public decimal ReadDecimal()
        {
            // TODO: Optimize this.
            var bits = new int[4];

            for (int i = 0; i < 4; i++)
                bits[i] = ReadInt32();

            return new decimal(bits);
        }

        #endregion
    }
}
