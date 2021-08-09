using ABCo.ABSave.Converters;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ABCo.ABSave.Deserialization
{
    /// <summary>
    /// Reads data bit-by-bit. Typically used to read from the header for an item, but can be created by a converter to allow more manual bit reading/writing.
    /// </summary>
    public class BitReader
    {
        ABSaveDeserializer _deserializer;
        public byte FreeBits;
        int _source;

        public CurrentState State => _deserializer.State;
        public byte CurrentByte => (byte)_source;

        public BitReader(ABSaveDeserializer deserializer) => _deserializer = deserializer;

        public void SetupHeader()
        {
            FreeBits = 0;
        }

        public bool ReadBit()
        {
            if (FreeBits == 0) MoveToNewByte();
            return (_source & (1 << --FreeBits)) > 0;
        }

        public byte ReadInteger(byte bitsRequired)
        {
            if (bitsRequired > FreeBits)
            {
                int remainingFromFirst = bitsRequired - FreeBits;
                int firstByteData = (_source & ABSaveUtils.IntFillMap[bitsRequired]) << remainingFromFirst;

                MoveToNewByte();

                FreeBits -= (byte)remainingFromFirst;
                int secondByteData = (_source >> FreeBits) & ABSaveUtils.IntFillMap[bitsRequired];
                return (byte)(firstByteData | secondByteData);
            }
            else
            {
                FreeBits -= bitsRequired;
                return (byte)((_source >> FreeBits) & ABSaveUtils.IntFillMap[bitsRequired]);
            }
        }

        public uint ReadCompressedInt() => (uint)CompressedDeserializer.ReadCompressed(false, this);
        public ulong ReadCompressedLong() => CompressedDeserializer.ReadCompressed(true, this);

        public string ReadString() => TextDeserializer.ReadNonNullString(this);
        public T ReadUTF8<T>(Func<int, T> createDest, Func<T, Memory<char>> castDest) => TextDeserializer.ReadUTF8<T>(createDest, castDest, this);

        public object? ReadItem(MapItemInfo info) => ItemDeserializer.DeserializeItem(info, this);
        public object? ReadExactNonNullItem(MapItemInfo info) => ItemDeserializer.DeserializeExactNonNullItem(info, this);

        public VersionInfo ReadAndStoreVersionNumber(Converter converter) => ItemDeserializer.HandleVersionNumber(converter, this);

        public void MoveToNewByte()
        {
            _source = _deserializer.ReadByte();
            FreeBits = 8;
        }

        public ABSaveDeserializer Finish()
        {
            FreeBits = 0;
            return _deserializer;
        }
    }

    //[StructLayout(LayoutKind.Auto)]
    //public struct BitReader
    //{
    //    CurrentBitReader _currentBitReader;
    //    public int FreeBits => _currentBitReader.FreeBits;

    //    internal BitReader(CurrentBitReader currentReader)
    //    {
    //        _currentBitReader = currentReader;
    //    }

    //    public bool ReadBit() => _currentBitReader.ReadBit();
    //    public byte ReadInteger(byte bitsRequired) => _currentBitReader.ReadInteger(bitsRequired);
    //    public ABSaveDeserializer Finish()
    //    {

    //    }
    //}
}
