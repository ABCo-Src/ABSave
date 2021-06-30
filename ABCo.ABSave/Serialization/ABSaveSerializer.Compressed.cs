using System;

namespace ABCo.ABSave.Serialization
{
    // ===============================================
    // NOTE: Full details about the "compressed numerical" structure can be seen in the TXT file: CompressedPlan.txt in the project root
    // ===============================================
    // This is just an implementation of everything shown there.
    public sealed partial class ABSaveSerializer
    {
        public void WriteCompressed(uint data)
        {
            var target = new BitTarget(this);
            WriteCompressed(data, ref target);
        }

        public void WriteCompressed(ulong data)
        {
            var target = new BitTarget(this);
            WriteCompressed(data, ref target);
        }

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

        public void WriteCompressed(uint data, ref BitTarget target) => WriteCompressed((ulong)data, ref target);
        public void WriteCompressed(ulong data, ref BitTarget target)
        {
            if (target.FreeBits == 0) target.Apply();

            var dataInfo = GetCompressedDataInfo(data, target.FreeBits);

            // Write the first byte
            WriteFirstByte(ref target, data, dataInfo);

            // Write the data in the remaining bytes
            while (dataInfo.BitsToGo > 0)
            {
                dataInfo.BitsToGo -= 8;
                WriteByte((byte)(data >> dataInfo.BitsToGo));
            }
        }

        void WriteFirstByte(ref BitTarget target, ulong data, CompressedDataInfo dataInfo)
        {
            bool isExtendedByte = target.FreeBits < 4;
            bool byteWillHaveFreeSpace = dataInfo.HeaderLen < target.FreeBits;

            // Write the header
            target.WriteInteger(dataInfo.Header, dataInfo.HeaderLen);

            // Handle extended starts (yyy-xxxxxxxx)
            if (isExtendedByte)
            {
                // Write any free "y"s.
                if (byteWillHaveFreeSpace) target.WriteInteger((byte)(data >> dataInfo.BitsToGo >> 8), target.FreeBits);

                // The next byte will definitely have some free space, as we can not physically fill all of the remaining "xxxxxxxx"s with the header.
                // Ensure we're definitely ready for the next byte.
                if (target.FreeBits == 0) target.Apply();

                byteWillHaveFreeSpace = true;
            }

            if (byteWillHaveFreeSpace) target.WriteInteger((byte)(data >> dataInfo.BitsToGo), target.FreeBits);

            target.Apply();
        }

        CompressedDataInfo GetCompressedDataInfo(ulong num, byte bitsFree)
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
