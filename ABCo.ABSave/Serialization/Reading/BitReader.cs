using ABCo.ABSave.Serialization.Converters;
using ABCo.ABSave.Serialization.Reading.Core;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ABCo.ABSave.Serialization.Reading
{
    /// <summary>
    /// Reads data bit-by-bit. Typically used to read from the header for an item, but can be created by a converter to allow more manual bit reading/writing.
    /// </summary>
    internal struct BitReader
    {
        ABSaveDeserializer _deserializer;
        public byte FreeBits { get; private set; }
        int _source;

        public DeserializeCurrentState State => _deserializer.State;
        public byte CurrentByte => (byte)_source;

        public BitReader(ABSaveDeserializer deserializer)
        {
            _deserializer = deserializer;
            FreeBits = 0;
            _source = 0;
        }

        public void Reset() => FreeBits = 0;

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
    //public struct ABSaveDeserializer
    //{
    //    CurrentABSaveDeserializer _currentABSaveDeserializer;
    //    public int FreeBits => _currentABSaveDeserializer.FreeBits;

    //    internal ABSaveDeserializer(CurrentABSaveDeserializer currentReader)
    //    {
    //        _currentABSaveDeserializer = currentReader;
    //    }

    //    public bool ReadBit() => _currentABSaveDeserializer.ReadBit();
    //    public byte ReadInteger(byte bitsRequired) => _currentABSaveDeserializer.ReadInteger(bitsRequired);
    //    public ABSaveDeserializer Finish()
    //    {

    //    }
    //}
}
