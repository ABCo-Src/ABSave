using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ABCo.ABSave.Serialization
{
    [StructLayout(LayoutKind.Auto)]
    // NOTE: This would benefit HUGELY with IL generation
    public struct BitTarget
    {
        public ABSaveSerializer Serializer;
        public int Result;
        public byte FreeBits;

        public BitTarget(ABSaveSerializer serializer, byte freeBits = 8)
        {
            FreeBits = freeBits;
            Serializer = serializer;

            Result = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBitOn()
        {
            if (FreeBits == 0) Apply();
            Result |= 1 << --FreeBits;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBitOff()
        {
            if (FreeBits == 0) Apply();
            FreeBits--;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBitWith(bool value)
        {
            if (value) WriteBitOn();
            else WriteBitOff();
        }

        public void WriteInteger(byte number, byte bitsRequired)
        {
            if (bitsRequired > FreeBits)
            {
                // If we're being lazy, or there's literally 0 bits free, just go onto a new byte instead of spending a lot of effort spreading it between two bytes.
                if (Serializer.Settings.LazyBitHandling || FreeBits == 0)
                {
                    Apply();
                    WriteInteger(number, bitsRequired);
                    return;
                }

                byte remainingFromFirst = (byte)(bitsRequired - FreeBits);
                Result |= number >> remainingFromFirst;

                Apply();

                FreeBits -= remainingFromFirst;
                Result |= number << FreeBits;
            }
            else
            {
                FreeBits -= bitsRequired;
                Result |= number << FreeBits;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OrAndApply(byte orWith)
        {
            Result |= orWith;
            Apply();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Apply()
        {
            Serializer.WriteByte((byte)Result);
            Result = 0;
            FreeBits = 8;
        }
    }
}
