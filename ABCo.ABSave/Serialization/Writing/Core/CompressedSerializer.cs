using ABCo.ABSave.Helpers.NumberContainer;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Serialization.Writing.Core
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
        public static void WriteCompressedInt(uint data, BitWriter target) => WriteCompressed(new Int32Container((int)data), target);
        public static void WriteCompressedLong(ulong data, BitWriter target) => WriteCompressed(new Int64Container((long)data), target);

        static void WriteCompressed<T>(T data, BitWriter target) where T : INumberContainer
        {
            if (target.FreeBits == 0) target.MoveToNextByte();

            if (target.State.Settings.LazyCompressedWriting)
                WriteCompressedLazyFast(data, target);
            else
                WriteCompressedSlow(data, target);
        }

        static void WriteCompressedLazyFast<T>(T data, BitWriter target) where T : INumberContainer
        {
            // This should be as blazing fast as possible, the hope is a lot of the work here will disappear with code-gen.
            // If the header is big enough, we'll try to fit the value into the rest of the header
            // and if it doesn't fit, we'll just straight write it.
            if (target.FreeBits > 3)
            {
                if (data.LessThan(1 << target.FreeBits >> 1))
                {
                    target.WriteBitOn();
                    target.FillRemainingWith(data.ToByte());
                }
                else
                {
                    target.WriteBitOff();
                    WriteFull(target.Finish());
                }
            }

            // If the header is so small it wouldn't be practical to try and fit in, we'll instead try to
            // fit it into a single byte, and write the full thing if we can't.
            else
            {
                if (data.LessThan(255))
                {
                    target.FillRemainingWith(1);
                    target.Finish().WriteByte(data.ToByte());
                }
                else
                {
                    target.FillRemainingWith(0);
                    WriteFull(target.Finish());
                }
            }

            void WriteFull(ABSaveSerializer serializer)
            {
                // This check is optimized away by the JIT.
                if (typeof(T) == typeof(Int32Container))
                    serializer.WriteInt32(data.ToInt32());
                else if (typeof(T) == typeof(Int64Container))
                    serializer.WriteInt64(data.ToInt64());
            }
        }

        static void WriteCompressedSlow<T>(T data, BitWriter target) where T : INumberContainer
        {
            var dataInfo = GetCompressedDataInfo(data, target.FreeBits);

            // Write the first byte
            WriteFirstByte(target, data, dataInfo);

            var serializer = target.Finish();

            // Write the data in the remaining bytes
            while (dataInfo.BitsToGo > 0)
            {
                dataInfo.BitsToGo -= 8;
                serializer.WriteByte((byte)data.ShiftRight(dataInfo.BitsToGo));
            }
        }

        static void WriteFirstByte<T>(BitWriter target, T data, CompressedDataInfo dataInfo) where T : INumberContainer
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
                if (target.FreeBits == 0) target.MoveToNextByte();

                byteWillHaveFreeSpace = true;
            }

            if (byteWillHaveFreeSpace) target.WriteInteger((byte)data.ShiftRight(dataInfo.BitsToGo), target.FreeBits);
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
