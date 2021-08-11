using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Serialization.Reading.Core
{
    internal static class CompressedDeserializer
    {
        public static ulong ReadCompressed(bool canBeLong, BitReader source)
        {
            if (source.FreeBits == 0) source.MoveToNewByte();

            if (source.State.Settings.LazyCompressedWriting)
                return ReadCompressedLazyFast(canBeLong, source);
            else
                return ReadCompressedSlow(source);
        }

        public static ulong ReadCompressedLazyFast(bool canBeLong, BitReader source)
        {
            // If the header is big enough, the value will have been fit into the rest of the header.
            if (source.FreeBits > 3)
            {
                if (source.ReadBit())
                    return source.ReadInteger(source.FreeBits);
                else
                    return ReadFull(source.Finish());
            }

            // If not, it may be in its own byte.
            else
            {
                bool isSingleByte = (source.CurrentByte & 1) > 0;

                var deserializer = source.Finish();

                if (isSingleByte)
                    return deserializer.ReadByte();
                else
                    return ReadFull(deserializer);
            }

            ulong ReadFull(ABSaveDeserializer deserializer)
            {
                if (canBeLong)
                    return (ulong)deserializer.ReadInt64();
                else
                    return (ulong)deserializer.ReadInt32();
            }
        }

        public static ulong ReadCompressedSlow(BitReader source)
        {
            // Process header
            byte preHeaderCapacity = source.FreeBits;
            (byte noContBytes, byte headerLen) = ReadNoContBytes(source);

            byte bitsToGo = (byte)(8 * noContBytes);
            ulong res = ReadFirstByteData(source, headerLen, bitsToGo, preHeaderCapacity);

            var deserializer = source.Finish();

            while (bitsToGo > 0)
            {
                bitsToGo -= 8;
                res |= (ulong)deserializer.ReadByte() << bitsToGo;
            }

            return res;
        }

        static ulong ReadFirstByteData(BitReader source, byte headerLen, byte noContBits, byte preHeaderCapacity)
        {
            bool isExtended = preHeaderCapacity < 4;
            ulong res = 0;

            // For an extended first byte (yyy-xxxxxxxx)
            if (isExtended)
            {
                // If there are still "y" bits left, get them.
                if (headerLen < preHeaderCapacity) res = (ulong)source.ReadInteger(source.FreeBits) << noContBits << 8;

                // Make sure we're ready to read "x"s. There will always be "x"es as the header can not physically take them all up.
                if (source.FreeBits == 0) source.MoveToNewByte();
            }

            return res | ((ulong)source.ReadInteger(source.FreeBits) << noContBits);
        }

        static (byte noContBytes, byte headerLen) ReadNoContBytes(BitReader source)
        {
            for (byte i = 0; i < 8; i++)
            {
                if (!source.ReadBit()) return (i, (byte)(i + 1));
            }

            return (8, 8);
        }
    }
}
