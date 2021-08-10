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
    public class BitWriter : IDisposable
    {
        ABSaveSerializer _serializer;
        public byte FreeBits;
        int _target;

        public SerializeCurrentState State => _serializer.State;
        internal BitWriter(ABSaveSerializer serializer) => _serializer = serializer;

        internal void SetupHeader()
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

        public void WriteSettingsHeaderIfNeeded() => HeaderSerializer.WriteHeader(this);
        public void WriteRoot(object? obj) => ItemSerializer.SerializeItem(obj, State.Map._rootItem, this);

        public void WriteItem(object? obj, MapItemInfo info) => ItemSerializer.SerializeItem(obj, info, this);
        public void WriteExactNonNullItem(object obj, MapItemInfo info) => ItemSerializer.SerializeExactNonNullItem(obj, info, this);

        public void WriteCompressedInt(uint data) => CompressedSerializer.WriteCompressedInt(data, this);
        public void WriteCompressedLong(ulong data) => CompressedSerializer.WriteCompressedLong(data, this);
        public void WriteNullableString(string? str) => TextSerializer.WriteString(str, this);
        public void WriteNonNullString(string str) => TextSerializer.WriteNonNullString(str, this);

        public void WriteText(ReadOnlySpan<char> bytes) => TextSerializer.WriteText(bytes, this);
        public void WriteUTF8(ReadOnlySpan<char> bytes) => TextSerializer.WriteUTF8(bytes, this);

        public VersionInfo WriteExactNonNullHeader(object obj, Type actualType, Converter converter) =>
            ItemSerializer.SerializeConverterHeader(obj, converter, actualType, true, this)!;

        public void MoveToNextByte()
        {
            _serializer.WriteByte((byte)_target);
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
