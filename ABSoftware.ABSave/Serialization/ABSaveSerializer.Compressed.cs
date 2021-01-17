using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Serialization
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

        public void WriteCompressed(uint data, ref BitTarget target) => WriteCompressedAndApply(data, ref target);
        public void WriteCompressed(ulong data, ref BitTarget target) => WriteCompressedAndApply(data, ref target);

        void WriteCompressedAndApply(ulong data, ref BitTarget target)
        {
            if (target.FreeBits == 0) target.Apply();

            byte contBytesNo = GetContBytesNo(data, target.FreeBits);
            byte bitsToGo = (byte)(8 * contBytesNo);

            // Write the first byte
            WriteFirstByte(ref target, data, bitsToGo, contBytesNo);

            // Write the data in the remaining bytes
            while (bitsToGo > 0)
            {
                bitsToGo -= 8;
                WriteByte((byte)(data >> bitsToGo));
            }
        }

        void WriteFirstByte(ref BitTarget target, ulong data, byte noOfContBits, byte noOfContBytes)
        {
            (byte header, byte headerLen) = GetHeader(noOfContBytes);

            bool isExtendedByte = target.FreeBits < 4;
            bool byteWillHaveFreeSpace = headerLen < target.FreeBits;

            // Write the header
            target.WriteInteger(header, headerLen);

            // Handle extended starts (yyy-xxxxxxxx)
            if (isExtendedByte)
            {
                // Write any free "y"s.
                if (byteWillHaveFreeSpace) target.WriteInteger((byte)(data >> noOfContBits >> 8), target.FreeBits);

                // The next byte will definitely have some free space, as we can not physically fill all of the remaining "xxxxxxxx"s with the header.
                // Ensure we're definitely ready for the next byte.
                if (target.FreeBits == 0) target.Apply();

                byteWillHaveFreeSpace = true;
            }

            if (byteWillHaveFreeSpace) target.WriteInteger((byte)(data >> noOfContBits), target.FreeBits);

            target.Apply();
        }

        private static (byte header, byte headerLen) GetHeader(byte contBytesRequired)
        {
            return contBytesRequired switch
            {
                0 => (0, 1),
                1 => (0b10, 2),
                2 => (0b110, 3),
                3 => (0b1110, 4),
                4 => (0b11110, 5),
                5 => (0b111110, 6),
                6 => (0b1111110, 7),
                7 => (0b11111110, 8),
                8 => (0b11111111, 8),
                _ => throw new Exception("ABSAVE: Invalid 'contBytesRequired' given to 'WriteVariableData'")
            };
        }

        // Methods to get the number of extra continuation bytes required after the header byte, for each of the supported "vriable" sizes. This does NOT include the header.

        byte GetContBytesNo(ulong num, byte bitsFree)
        {
            ulong mask = (1UL << bitsFree) >> 1;

            // Extended byte
            if (bitsFree < 4) mask <<= 8;

            if (num < mask) return 0;
            else if (num < (mask << 7)) return 1;
            else if (num < (mask << 14)) return 2;
            else if (num < (mask << 21)) return 3;
            else if (num < (mask << 28)) return 4;
            else if (num < (mask << 35)) return 5;
            else if (num < (mask << 42)) return 6;
            else if (num < (mask << 49)) return 7;
            else return 8;
        }

        byte Get8BitContBytesNo(ulong num)
        {
            if (num < (1 << 7)) return 0;
            else if (num < (1 << 14)) return 1;
            else if (num < (1 << 21)) return 2;
            else if (num < (1 << 28)) return 3;
            else if (num < ((ulong)1 << 35)) return 4;
            else if (num < ((ulong)1 << 42)) return 5;
            else if (num < ((ulong)1 << 49)) return 6;
            else if (num < ((ulong)1 << 56)) return 7;
            else return 8;
        }

        byte Get7BitContBytesNo(ulong num)
        {
            if (num < (1 << 6)) return 0;
            else if (num < (1 << 13)) return 1;
            else if (num < (1 << 20)) return 2;
            else if (num < (1 << 27)) return 3;
            else if (num < ((ulong)1 << 33)) return 4;
            else if (num < ((ulong)1 << 40)) return 5;
            else if (num < ((ulong)1 << 47)) return 6;
            else if (num < ((ulong)1 << 55)) return 6;
            else return 8;
        }

        byte Get6BitContBytesNo(ulong num)
        {
            if (num < (1 << 5)) return 0;
            else if (num < (1 << 12)) return 1;
            else if (num < (1 << 19)) return 2;
            else if (num < (1 << 26)) return 3;
            else if (num < ((ulong)1 << 33)) return 4;
            else if (num < ((ulong)1 << 40)) return 5;
            else if (num < ((ulong)1 << 47)) return 6;
            else return 8;
        }

        byte Get5BitContBytesNo(ulong num)
        {
            if (num < (1 << 4)) return 0;
            else if (num < (1 << 11)) return 1;
            else if (num < (1 << 18)) return 2;
            else if (num < (1 << 25)) return 3;
            else if (num < ((ulong)1 << 32)) return 4;
            else if (num < ((ulong)1 << 39)) return 4;
            else if (num < ((ulong)1 << 46)) return 5;
            else if (num < ((ulong)1 << 53)) return 6;
            else return 8;
        }

        byte Get4BitContBytesNo(ulong num)
        {
            if (num < (1 << 3)) return 0;
            if (num < (1 << 10)) return 1;
            else if (num < (1 << 17)) return 2;
            else if (num < (1 << 24)) return 3;
            else if (num < ((ulong)1 << 31)) return 3;
            else if (num < ((ulong)1 << 38)) return 4;
            else if (num < ((ulong)1 << 45)) return 5;
            else if (num < ((ulong)1 << 52)) return 6;
            else return 8;
        }

        byte Get3BitContBytesNo(ulong num)
        {
            if (num < (1 << 10)) return 0;
            else if (num < (1 << 17)) return 1;
            else if (num < (1 << 24)) return 2;
            else if (num < ((ulong)1 << 31)) return 3;
            else if (num < ((ulong)1 << 38)) return 4;
            else if (num < ((ulong)1 << 45)) return 5;
            else if (num < ((ulong)1 << 53)) return 6;
            else if (num < ((ulong)1 << 59)) return 7;
            else return 8;
        }

        byte Get2BitContBytesNo(ulong num)
        {
            if (num < (1 << 9)) return 0;
            else if (num < (1 << 16)) return 1;
            else if (num < (1 << 23)) return 2;
            else if (num < ((ulong)1 << 30)) return 3;
            else if (num < ((ulong)1 << 37)) return 4;
            else if (num < ((ulong)1 << 44)) return 5;
            else if (num < ((ulong)1 << 51)) return 6;
            else if (num < ((ulong)1 << 58)) return 7;
            else return 8;
        }

        byte Get1BitContBytesNo(ulong num)
        {
            if (num < (1 << 8)) return 0;
            else if (num < (1 << 15)) return 1;
            else if (num < (1 << 22)) return 2;
            else if (num < (1 << 29)) return 3;
            else if (num < (1 << 36)) return 4;
            else if (num < (1 << 43)) return 5;
            else if (num < (1 << 50)) return 6;
            else if (num < (1 << 57)) return 7;
            else return 8;
        }
    }
}
