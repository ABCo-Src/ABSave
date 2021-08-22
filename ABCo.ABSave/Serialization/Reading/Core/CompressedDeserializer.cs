using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Serialization.Reading.Core
{
    internal static class CompressedDeserializer
    {
        public static ulong ReadCompressed(bool canBeLong, ABSaveDeserializer source)
        {
            if (source.CurrentByteFreeBits == 0) source.MoveToNewCurrentByte();

            if (source.State.Settings.LazyCompressedWriting)
                return ReadCompressedLazyFast(canBeLong, source);
            else
                return ReadCompressedSlow(source);
        }

        public static ulong ReadCompressedLazyFast(bool canBeLong, ABSaveDeserializer source)
        {
            // If the header is big enough, the value will have been fit into the rest of the header.
            if (source.CurrentByteFreeBits > 3)
            {
                if (source.ReadBit())
                    return source.ReadInteger(source.CurrentByteFreeBits);
                else
                    return ReadFull(source);
            }

            // If not, it may be in its own byte.
            else
            {
                bool isSingleByte = (source.GetCurrentByte() & 1) > 0;

                if (isSingleByte)
                    return source.ReadByte();
                else
                    return ReadFull(source);
            }

            ulong ReadFull(ABSaveDeserializer deserializer)
            {
                if (canBeLong)
                    return (ulong)deserializer.ReadInt64();
                else
                    return (ulong)deserializer.ReadInt32();
            }
        }

        public static ulong ReadCompressedSlow(ABSaveDeserializer source)
        {
            // Process header
            byte preHeaderCapacity = source.CurrentByteFreeBits;
            (byte noContBytes, byte headerLen) = ReadNoContBytes(source);

            byte bitsToGo = (byte)(8 * noContBytes);
            ulong res = ReadFirstByteData(source, headerLen, bitsToGo, preHeaderCapacity);

            while (bitsToGo > 0)
            {
                bitsToGo -= 8;
                res |= (ulong)source.ReadByte() << bitsToGo;
            }

            return res;
        }

        static ulong ReadFirstByteData(ABSaveDeserializer source, byte headerLen, byte noContBits, byte preHeaderCapacity)
        {
            bool isExtended = preHeaderCapacity < 4;
            ulong res = 0;

            // For an extended first byte (yyy-xxxxxxxx)
            if (isExtended)
            {
                // If there are still "y" bits left, get them.
                if (headerLen < preHeaderCapacity) res = (ulong)source.ReadInteger(source.CurrentByteFreeBits) << noContBits << 8;

                // Make sure we're ready to read "x"s. There will always be "x"es as the header can not physically take them all up.
                if (source.CurrentByteFreeBits == 0) source.MoveToNewCurrentByte();
            }

            return res | ((ulong)source.ReadInteger(source.CurrentByteFreeBits) << noContBits);
        }

        static (byte noContBytes, byte headerLen) ReadNoContBytes(ABSaveDeserializer source)
        {
            for (byte i = 0; i < 8; i++)
            {
                if (!source.ReadBit()) return (i, (byte)(i + 1));
            }

            return (8, 8);
        }
    }
}
