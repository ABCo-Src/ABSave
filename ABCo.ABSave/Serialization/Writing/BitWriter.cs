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
    internal struct BitWriter : IDisposable
    {
        readonly ABSaveSerializer _serializer;

        public byte FreeBits => (byte)(_freeBits == 0 ? 8 : _freeBits);
        byte _freeBits;
        int _target;

        public SerializeCurrentState State => _serializer.State;
        internal BitWriter(ABSaveSerializer serializer)
        {
            _serializer = serializer;
            _freeBits = 0;
            _target = 0;
        }

        internal void Reset()
        {
            _freeBits = 8;
            _target = 0;
        }

        public void WriteBitOn()
        {
            if (_freeBits == 0) MoveToNextByte();
            _target |= 1 << --_freeBits;
        }

        public void WriteBitOff()
        {
            if (_freeBits == 0) MoveToNextByte();
            _freeBits--;
        }

        public void WriteBitWith(bool value)
        {
            if (value) WriteBitOn();
            else WriteBitOff();
        }

        public void WriteInteger(byte number, byte bitsRequired)
        {
            if (bitsRequired > _freeBits)
            {
                byte remainingFromFirst = (byte)(bitsRequired - _freeBits);
                _target |= number >> remainingFromFirst;

                MoveToNextByte();

                _freeBits -= remainingFromFirst;
                _target |= number << _freeBits;
            }
            else
            {
                _freeBits -= bitsRequired;
                _target |= number << _freeBits;
            }
        }

        public void MoveToNextByte()
        {
            _serializer.WriteByteUnchecked((byte)_target);
            _target = 0;
            _freeBits = 8;
        }

        public void FillRemainingWith(int n)
        {
            if (_freeBits == 0) MoveToNextByte();

            _target |= n;
            _freeBits = 0;
        }

        public ABSaveSerializer Finish()
        {
            if (_freeBits != 8) MoveToNextByte();
            return _serializer;
        }

        public void Dispose() => Finish();
    }
}
