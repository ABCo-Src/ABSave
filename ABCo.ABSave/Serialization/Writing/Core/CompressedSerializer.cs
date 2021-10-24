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

        public static void WriteCompressedSigned<TNumType>(long data, ABSaveSerializer serializer)
        {
            if (data < 0)
            {
                serializer.WriteBitOn();
                WriteCompressed<TNumType>((ulong)-data, serializer);
            }
            else
            {
                serializer.WriteBitOff();
                WriteCompressed<TNumType>((ulong)data, serializer);
            }
        }

        public static void WriteCompressed<TNumType>(ulong data, ABSaveSerializer target)
        {
            if (target.State.Settings.LazyCompressedWriting)
                WriteCompressedLazyFast<TNumType>(data, target);
            else
                WriteCompressedSlow(data, target);
        }

        static void WriteCompressedLazyFast<TNumType>(ulong data, ABSaveSerializer serializer)
        {
            // This should be as blazing fast as possible, the hope is a lot of the work here will disappear with code-gen.
            // If the header is big enough, we'll try to fit the value into the rest of the header
            // and if it doesn't fit, we'll just straight write it.
            if (serializer.CurrentByteFreeBits > 3)
            {
                if (data < (ulong)(1 << serializer.CurrentByteFreeBits >> 1))
                {
                    serializer.WriteBitOn();
                    serializer.FillRestOfCurrentByteWith((byte)data);
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
                if (data < 255)
                {
                    serializer.FillRestOfCurrentByteWith(1);
                    serializer.WriteByte((byte)data);
                }
                else
                {
                    serializer.FillRestOfCurrentByteWith(0);
                    WriteFull(serializer);
                }
            }

            void WriteFull(ABSaveSerializer serializer)
            {
                // This check is optimized away by the JIT.
                if (typeof(TNumType) == typeof(uint))
                    serializer.WriteInt32((int)data);
                else if (typeof(TNumType) == typeof(ulong))
                    serializer.WriteInt64((long)data);
            }
        }

        static void WriteCompressedSlow(ulong data, ABSaveSerializer serializer)
        {
            var dataInfo = GetCompressedDataInfo(data, serializer.CurrentByteFreeBits);

            // Write the first byte
            WriteFirstByte(serializer, data, dataInfo);

            // Write the data in the remaining bytes
            while (dataInfo.BitsToGo > 0)
            {
                dataInfo.BitsToGo -= 8;
                serializer.WriteByte((byte)(data >> dataInfo.BitsToGo));
            }
        }

        static void WriteFirstByte(ABSaveSerializer serializer, ulong data, CompressedDataInfo dataInfo)
        {
            bool isExtendedByte = serializer.CurrentByteFreeBits < 4;
            bool byteWillHaveFreeSpace = dataInfo.HeaderLen < serializer.CurrentByteFreeBits;

            // Write the header
            serializer.WriteInteger(dataInfo.Header, dataInfo.HeaderLen);

            // Handle extended starts (yyy-xxxxxxxx)
            if (isExtendedByte)
            {
                // Write any free "y"s.
                if (byteWillHaveFreeSpace) serializer.WriteInteger((byte)(data >> dataInfo.BitsToGo >> 8), serializer.CurrentByteFreeBits);

                // The next byte will definitely have some free space, as we can not physically fill all of the remaining "xxxxxxxx"s with the header.
                byteWillHaveFreeSpace = true;
            }

            if (byteWillHaveFreeSpace) serializer.WriteInteger((byte)(data >> dataInfo.BitsToGo), serializer.CurrentByteFreeBits);
        }

        static CompressedDataInfo GetCompressedDataInfo(ulong num, byte bitsFree)
        {
            ulong mask = (1UL << bitsFree) >> 1;

            // Extended byte
            if (bitsFree < 4) mask <<= 8;

            if (num < mask) return new CompressedDataInfo(0, 0, 1);
            else if (num < (mask << 7)) return new CompressedDataInfo(1, 0b10, 2);
            else if (num < (mask << 14)) return new CompressedDataInfo(2, 0b110, 3);
            else if (num < (mask << 21)) return new CompressedDataInfo(3, 0b1110, 4);
            else if (num < (mask << 28)) return new CompressedDataInfo(4, 0b11110, 5);
            else if (num < (mask << 35)) return new CompressedDataInfo(5, 0b111110, 6);
            else if (num < (mask << 42)) return new CompressedDataInfo(6, 0b1111110, 7);
            else if (num < (mask << 49)) return new CompressedDataInfo(7, 0b11111110, 8);
            else return new CompressedDataInfo(8, 255, 8);
        }
    }
}
