using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Serialization.Reading.Core
{
    internal static class CompressedDeserializer
    {
        public static ulong ReadCompressed(bool canBeLong, ABSaveDeserializer deserializer)
        {
            if (deserializer.CurrentByteFreeBits == 0) deserializer.MoveToNewCurrentByte();

            if (deserializer.State.Settings.LazyCompressedWriting)
                return ReadCompressedLazyFast(canBeLong, deserializer);
            else
                return ReadCompressedSlow(deserializer);
        }

        public static ulong ReadCompressedLazyFast(bool canBeLong, ABSaveDeserializer deserializer)
        {
            // If the header is big enough, the value will have been fit into the rest of the header.
            if (deserializer.CurrentByteFreeBits > 3)
            {
                if (deserializer.ReadBit())
                    return deserializer.ReadInteger(deserializer.CurrentByteFreeBits);
                else
                    return ReadFull(deserializer);
            }

            // If not, it may be in its own byte.
            else
            {
                bool isSingleByte = (deserializer.GetCurrentByte() & 1) > 0;

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

        public static ulong ReadCompressedSlow(ABSaveDeserializer deserializer)
        {
            // Process header
            byte preHeaderCapacity = deserializer.CurrentByteFreeBits;
            (byte noContBytes, byte headerLen) = ReadNoContBytes(deserializer);

            byte bitsToGo = (byte)(8 * noContBytes);
            ulong res = ReadFirstByteData(deserializer, headerLen, bitsToGo, preHeaderCapacity);

            while (bitsToGo > 0)
            {
                bitsToGo -= 8;
                res |= (ulong)deserializer.ReadByte() << bitsToGo;
            }

            return res;
        }

        static ulong ReadFirstByteData(ABSaveDeserializer deserializer, byte headerLen, byte noContBits, byte preHeaderCapacity)
        {
            bool isExtended = preHeaderCapacity < 4;
            ulong res = 0;

            // For an extended first byte (yyy-xxxxxxxx)
            if (isExtended)
            {
                // If there are still "y" bits left, get them.
                if (headerLen < preHeaderCapacity) res = (ulong)deserializer.ReadInteger(deserializer.CurrentByteFreeBits) << noContBits << 8;

                // Make sure we're ready to read "x"s. There will always be "x"es as the header can not physically take them all up.
                if (deserializer.CurrentByteFreeBits == 0) deserializer.MoveToNewCurrentByte();
            }

            return res | ((ulong)deserializer.ReadInteger(deserializer.CurrentByteFreeBits) << noContBits);
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
