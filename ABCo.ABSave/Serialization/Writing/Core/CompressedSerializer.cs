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
        public static void WriteCompressedInt(uint data, ABSaveSerializer serializer) => WriteCompressed(new Int32Container((int)data), serializer);
        public static void WriteCompressedLong(ulong data, ABSaveSerializer serializer) => WriteCompressed(new Int64Container((long)data), serializer);

        static void WriteCompressed<T>(T data, ABSaveSerializer target) where T : INumberContainer
        {
            if (target.State.Settings.LazyCompressedWriting)
                WriteCompressedLazyFast(data, target);
            else
                WriteCompressedSlow(data, target);
        }

        static void WriteCompressedLazyFast<T>(T data, ABSaveSerializer serializer) where T : INumberContainer
        {
            // This should be as blazing fast as possible, the hope is a lot of the work here will disappear with code-gen.
            // If the header is big enough, we'll try to fit the value into the rest of the header
            // and if it doesn't fit, we'll just straight write it.
            if (serializer.CurrentByteFreeBits > 3)
            {
                if (data.LessThan(1 << serializer.CurrentByteFreeBits >> 1))
                {
                    serializer.WriteBitOn();
                    serializer.FillRemainderOfCurrentByteWith(data.ToByte());
                }
                else
                {
                    serializer.WriteBitOff();
                    WriteFull(serializer);
                }
            }

            // If the header is so small it wouldn't be practical to try and fit in, we'll instead try to
            // fit it into a single byte, and write the full thing if we can't.
            else
            {
                if (data.LessThan(255))
                {
                    serializer.FillRemainderOfCurrentByteWith(1);
                    serializer.WriteByte(data.ToByte());
                }
                else
                {
                    serializer.FillRemainderOfCurrentByteWith(0);
                    WriteFull(serializer);
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

        static void WriteCompressedSlow<T>(T data, ABSaveSerializer serializer) where T : INumberContainer
        {
            var dataInfo = GetCompressedDataInfo(data, serializer.CurrentByteFreeBits);

            // Write the first byte
            WriteFirstByte(serializer, data, dataInfo);

            // Write the data in the remaining bytes
            while (dataInfo.BitsToGo > 0)
            {
                dataInfo.BitsToGo -= 8;
                serializer.WriteByte((byte)data.ShiftRight(dataInfo.BitsToGo));
            }
        }

        static void WriteFirstByte<T>(ABSaveSerializer serializer, T data, CompressedDataInfo dataInfo) where T : INumberContainer
        {
            bool isExtendedByte = serializer.CurrentByteFreeBits < 4;
            bool byteWillHaveFreeSpace = dataInfo.HeaderLen < serializer.CurrentByteFreeBits;

            // Write the header
            serializer.WriteInteger(dataInfo.Header, dataInfo.HeaderLen);

            // Handle extended starts (yyy-xxxxxxxx)
            if (isExtendedByte)
            {
                // Write any free "y"s.
                if (byteWillHaveFreeSpace) serializer.WriteInteger((byte)(data.ShiftRight(dataInfo.BitsToGo) >> 8), serializer.CurrentByteFreeBits);

                // The next byte will definitely have some free space, as we can not physically fill all of the remaining "xxxxxxxx"s with the header.
                byteWillHaveFreeSpace = true;
            }

            if (byteWillHaveFreeSpace) serializer.WriteInteger((byte)data.ShiftRight(dataInfo.BitsToGo), serializer.CurrentByteFreeBits);
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
