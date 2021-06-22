using ABCo.ABSave.Helpers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ABCo.ABSave.Deserialization
{
    /// <summary>
    /// Represents data coming in bit-by-bit from a given source. This does not read anything until a bit is read.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public struct BitSource
    {
        public ABSaveDeserializer Deserializer;
        public int Source;
        public byte FreeBits;

        public BitSource(ABSaveDeserializer deserializer, byte freeBits = 8)
        {
            FreeBits = freeBits;
            Source = deserializer.ReadByte();
            Deserializer = deserializer;
        }

        public BitSource(byte source, ABSaveDeserializer deserializer, byte freeBits = 8)
        {
            FreeBits = freeBits;
            Source = source;
            Deserializer = deserializer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBit()
        {
            if (FreeBits == 0) MoveToNewByte();
            return (Source & (1 << --FreeBits)) > 0;
        }

        public byte ReadInteger(byte bitsRequired)
        {
            if (bitsRequired > FreeBits)
            {
                if (Deserializer.Settings.LazyBitHandling)
                {
                    MoveToNewByte();
                    return ReadInteger(bitsRequired);
                }

                int remainingFromFirst = bitsRequired - FreeBits;
                int firstByteData = (Source & ABSaveUtils.IntFillMap[bitsRequired]) << remainingFromFirst;

                MoveToNewByte();

                FreeBits -= (byte)remainingFromFirst;
                int secondByteData = (Source >> FreeBits) & ABSaveUtils.IntFillMap[bitsRequired];
                return (byte)(firstByteData | secondByteData);
            }
            else
            {
                FreeBits -= bitsRequired;
                return (byte)((Source >> FreeBits) & ABSaveUtils.IntFillMap[bitsRequired]);
            }
        }

        public void MoveToNewByte()
        {
            Source = Deserializer.ReadByte();
            FreeBits = 8;
        }
    }
}
