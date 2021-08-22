using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Serialization.Converters;
using ABCo.ABSave.Serialization.Writing.Core;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ABCo.ABSave.Serialization.Writing
{
    [StructLayout(LayoutKind.Auto)]
    internal class BitWriter : IDisposable
    {
        ABSaveSerializer _serializer;
        public byte FreeBits { get; private set; }
        int _target;

        public SerializeCurrentState State => _serializer.State;
        internal BitWriter(ABSaveSerializer serializer) => _serializer = serializer;

        internal void Reset()
        {
            FreeBits = 8;
            _target = 0;
        }

        public void WriteBitOn()
        {
            if (FreeBits == 0) MoveToNextByte();
            _target |= 1 << --FreeBits;
        }

        public void WriteBitOff()
        {
            if (FreeBits == 0) MoveToNextByte();
            FreeBits--;
        }

        public void WriteBitWith(bool value)
        {
            if (value) WriteBitOn();
            else WriteBitOff();
        }

        public void WriteInteger(byte number, byte bitsRequired)
        {
            if (bitsRequired > FreeBits)
            {
                byte remainingFromFirst = (byte)(bitsRequired - FreeBits);
                _target |= number >> remainingFromFirst;

                MoveToNextByte();

                FreeBits -= remainingFromFirst;
                _target |= number << FreeBits;
            }
            else
            {
                FreeBits -= bitsRequired;
                _target |= number << FreeBits;
            }
        }

        public void MoveToNextByte()
        {
            _serializer.WriteByteUnchecked((byte)_target);
            _target = 0;
            FreeBits = 8;
        }

        public void FillRemainingWith(int n)
        {
            _target |= n;
            FreeBits = 0;
        }

        public ABSaveSerializer Finish()
        {
            if (FreeBits != 8) MoveToNextByte();
            return _serializer;
        }

        public void Dispose() => Finish();
    }
}
