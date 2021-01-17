using System;
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
            NumericalReadBytes((byte*)&res, 2);
            return res;
        }

        public unsafe int ReadInt32()
        {
            int data = 0;
            NumericalReadBytes((byte*)&data, 4);
            return data;
        }

        public unsafe long ReadInt64()
        {
            long data = 0;
            NumericalReadBytes((byte*)&data, 8);
            return data;
        }

        public unsafe float ReadSingle()
        {
            float data = 0;
            NumericalReadBytes((byte*)&data, 4);
            return data;
        }

        public unsafe double ReadDouble()
        {
            double data = 0;
            NumericalReadBytes((byte*)&data, 8);
            return data;
        }

        public decimal ReadDecimal()
        {
            // TODO: Optimize this.
            var bits = new int[4];

            for (int i = 0; i < 4; i++)
                bits[i] = ReadInt32();

            return new decimal(bits);
        }

        unsafe void NumericalReadBytes(byte* data, int numberOfBytes)
        {
            if (ShouldReverseEndian)
            {
                byte* buffer = stackalloc byte[numberOfBytes];
                Source.Read(new Span<byte>(buffer, numberOfBytes));

                buffer += numberOfBytes;

                for (int i = 0; i < numberOfBytes; i++)
                    *data++ = *--buffer;
            }

            else Source.Read(new Span<byte>(data, numberOfBytes));
        }

        public unsafe int ReadLittleEndianInt32(int significantBytes)
        {
            int dest = 0;
            byte* destPos = (byte*)&dest;

            if (BitConverter.IsLittleEndian)
                Source.Read(new Span<byte>(destPos, significantBytes));
            else
            {
                byte* src = stackalloc byte[significantBytes];
                Source.Read(new Span<byte>(src, significantBytes));

                destPos += significantBytes;
                for (int i = 0; i < significantBytes; i++)
                    *--destPos = *src++;
            }

            return dest;
        }

        #endregion
    }
}
