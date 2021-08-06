using ABCo.ABSave.Helpers;
using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ABCo.ABSave.Deserialization
{
    public sealed partial class ABSaveDeserializer
    {
        #region Bit Reading

        readonly BitReader _currentBitReader;

        #endregion

        #region Byte Reading

        public byte ReadByte() => (byte)Source.ReadByte();
        public void ReadBytes(Span<byte> dest) => Source.Read(dest);
        public void ReadBytes(byte[] dest) => Source.Read(dest, 0, dest.Length);

        #endregion

        #region Numerical Reading

        public unsafe short ReadInt16()
        {
            short res = 0;
            ReadBytes(new Span<byte>((byte*)&res, 2));
            return State.ShouldReverseEndian ? BinaryPrimitives.ReverseEndianness(res) : res;
        }

        public unsafe int ReadInt32()
        {
            int res = 0;
            ReadBytes(new Span<byte>((byte*)&res, 4));
            return State.ShouldReverseEndian ? BinaryPrimitives.ReverseEndianness(res) : res;
        }

        public unsafe long ReadInt64()
        {
            long res = 0;
            ReadBytes(new Span<byte>((byte*)&res, 8));
            return State.ShouldReverseEndian ? BinaryPrimitives.ReverseEndianness(res) : res;
        }

        public unsafe float ReadSingle()
        {
            if (State.ShouldReverseEndian)
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
            if (State.ShouldReverseEndian)
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
            int[]? bits = new int[4];

            for (int i = 0; i < 4; i++)
                bits[i] = ReadInt32();

            return new decimal(bits);
        }

        #endregion
    }
}
