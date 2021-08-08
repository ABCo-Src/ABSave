using ABCo.ABSave.Helpers.NumberContainer;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Serialization
{
    internal static class CompressedSerializer
    {
        struct CompressedDataInfo
        {
            public byte ContBytesNo;
            public byte BitsToGo;
            public byte Header;
            public byte HeaderLen;

            public CompressedDataInfo(byte contBytesNo, byte header, byte headerLen)
            {
                (ContBytesNo, Header, HeaderLen) = (contBytesNo, header, headerLen);
                BitsToGo = (byte)(contBytesNo * 8);
            }
        }

        // NOTE: All uses of "INumberContainer" virtual calls in here are elided by the JIT thanks to generics.
        public static void WriteCompressedInt(uint data, ref BitTarget target) => WriteCompressed(new Int32Container((int)data), ref target);
        public static void WriteCompressedLong(ulong data, ref BitTarget target) => WriteCompressed(new Int64Container((long)data), ref target);

        static void WriteCompressed<T>(T data, ref BitTarget target) where T : INumberContainer
        {
            if (target.FreeBits == 0) target.Apply();

            if (target.State.Settings.LazyCompressedWriting)
                WriteCompressedLazyFast(data, ref target);
            else
                WriteCompressedSlow(data, ref target);
        }

        static void WriteCompressedLazyFast<T>(T data, ref BitTarget target) where T : INumberContainer
        {
            // This should be blazing fast, we're literally just going to write whether the number takes up more than a byte, and that's it.
            if (data.LessThan(255))
            {
                target.OrAndApply(1);
                target.Serializer.WriteByte(data.ToByte());
            }
            else
            {
                target.OrAndApply(0);

                // This check is optimized away by the JIT.
                if (typeof(T) == typeof(Int32Container))
                    target.Serializer.WriteInt32(data.ToInt32());
                else if (typeof(T) == typeof(Int64Container))
                    target.Serializer.WriteInt64(data.ToInt64());
            }
        }

        static void WriteCompressedSlow<T>(T data, ref BitTarget target) where T : INumberContainer
        {
            var dataInfo = GetCompressedDataInfo(data, target.FreeBits);

            // Write the first byte
            WriteFirstByte(ref target, data, dataInfo);

            // Write the data in the remaining bytes
            while (dataInfo.BitsToGo > 0)
            {
                dataInfo.BitsToGo -= 8;
                target.Serializer.WriteByte((byte)data.ShiftRight(dataInfo.BitsToGo));
            }
        }

        static void WriteFirstByte<T>(ref BitTarget target, T data, CompressedDataInfo dataInfo) where T : INumberContainer
        {
            bool isExtendedByte = target.FreeBits < 4;
            bool byteWillHaveFreeSpace = dataInfo.HeaderLen < target.FreeBits;

            // Write the header
            target.WriteInteger(dataInfo.Header, dataInfo.HeaderLen);

            // Handle extended starts (yyy-xxxxxxxx)
            if (isExtendedByte)
            {
                // Write any free "y"s.
                if (byteWillHaveFreeSpace) target.WriteInteger((byte)(data.ShiftRight(dataInfo.BitsToGo) >> 8), target.FreeBits);

                // The next byte will definitely have some free space, as we can not physically fill all of the remaining "xxxxxxxx"s with the header.
                // Ensure we're definitely ready for the next byte.
                if (target.FreeBits == 0) target.Apply();

                byteWillHaveFreeSpace = true;
            }

            if (byteWillHaveFreeSpace) target.WriteInteger((byte)data.ShiftRight(dataInfo.BitsToGo), target.FreeBits);

            target.Apply();
        }

        static CompressedDataInfo GetCompressedDataInfo<T>(T num, byte bitsFree) where T : INumberContainer
        {
            long mask = (1L << bitsFree) >> 1;

            // Extended byte
            if (bitsFree < 4) mask <<= 8;

            if (num.LessThanLong(mask)) return new CompressedDataInfo(0, 0, 1);
            else if (num.LessThanLong(mask << 7)) return new CompressedDataInfo(1, 0b10, 2);
            else if (num.LessThanLong(mask << 14)) return new CompressedDataInfo(2, 0b110, 3);
            else if (num.LessThanLong(mask << 21)) return new CompressedDataInfo(3, 0b1110, 4);
            else if (num.LessThanLong(mask << 28)) return new CompressedDataInfo(4, 0b11110, 5);
            else if (num.LessThanLong(mask << 35)) return new CompressedDataInfo(5, 0b111110, 6);
            else if (num.LessThanLong(mask << 42)) return new CompressedDataInfo(6, 0b1111110, 7);
            else if (num.LessThanLong(mask << 49)) return new CompressedDataInfo(7, 0b11111110, 8);
            else return new CompressedDataInfo(8, 255, 8);
        }
    }
}
