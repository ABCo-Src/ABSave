using ABCo.ABSave.Configuration;
using ABCo.ABSave.Serialization.Converters;
using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description;
using ABCo.ABSave.Mapping.Description.Attributes;
using ABCo.ABSave.Mapping.Generation.Inheritance;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using ABCo.ABSave.Serialization.Reading.Core;

namespace ABCo.ABSave.Serialization.Reading
{
    public sealed partial class ABSaveDeserializer : IDisposable
    {
        public Stream Source { get; private set; }
        public DeserializeCurrentState State { get; private set; }

        readonly BitReader _currentBitReader;

        internal ABSaveDeserializer(ABSaveMap map)
        {
            Source = null!;
            State = new DeserializeCurrentState(map);
            _currentBitReader = new BitReader(this);
        }

        public void Reset()
        {
            State.Reset();
            State.CachedKeys.Clear();
        }

        public void Dispose() => State.Map.ReleaseDeserializer(this);

        public void ReadSettingsHeaderIfNeeded() => HeaderDeserializer.ReadHeader(_currentBitReader);

        public uint ReadCompressedInt() => (uint)CompressedDeserializer.ReadCompressed(false, _currentBitReader);
        public ulong ReadCompressedLong() => CompressedDeserializer.ReadCompressed(true, _currentBitReader);

        public string? ReadNullableString() => TextDeserializer.ReadNullableString(_currentBitReader);
        public string ReadNonNullString() => TextDeserializer.ReadNonNullString(_currentBitReader);
        public T ReadUTF8<T>(Func<int, T> createDest, Func<T, Memory<char>> castDest) => TextDeserializer.ReadUTF8<T>(createDest, castDest, _currentBitReader);

        public object? ReadRoot() => ItemDeserializer.DeserializeItem(State.Map._rootItem, _currentBitReader);
        public object? ReadItem(MapItemInfo info) => ItemDeserializer.DeserializeItem(info, _currentBitReader);
        public object? ReadExactNonNullItem(MapItemInfo info) => ItemDeserializer.DeserializeExactNonNullItem(info, _currentBitReader);

        public VersionInfo ReadExactNonNullHeader(Converter converter)
        {
            ItemDeserializer.DeserializeConverterHeader(converter, _currentBitReader, true, out var info);
            return info;
        }

        #region Bit Reading

        public bool ReadBit() => _currentBitReader.ReadBit();
        public byte ReadInteger(byte bitsRequired) => _currentBitReader.ReadInteger(bitsRequired);

        #endregion

        #region Byte Reading

        public byte ReadByte() => (byte)Source.ReadByte();
        public void ReadBytes(Span<byte> dest) => Source.Read(dest);
        public void ReadBytes(byte[] dest) => Source.Read(dest, 0, dest.Length);

        #endregion

        #region Numerical Reading

        public unsafe short ReadInt16()
        {
            short res = 0;
            ReadBytes(new Span<byte>((byte*)&res, 2));
            return State.ShouldReverseEndian ? BinaryPrimitives.ReverseEndianness(res) : res;
        }

        public unsafe int ReadInt32()
        {
            int res = 0;
            ReadBytes(new Span<byte>((byte*)&res, 4));
            return State.ShouldReverseEndian ? BinaryPrimitives.ReverseEndianness(res) : res;
        }

        public unsafe long ReadInt64()
        {
            long res = 0;
            ReadBytes(new Span<byte>((byte*)&res, 8));
            return State.ShouldReverseEndian ? BinaryPrimitives.ReverseEndianness(res) : res;
        }

        public unsafe float ReadSingle()
        {
            if (State.ShouldReverseEndian)
            {
                int res = 0;
                ReadBytes(new Span<byte>((byte*)&res, 4));
                return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReverseEndianness(res));
            }
            else
            {
                float res = 0;
                ReadBytes(new Span<byte>((byte*)&res, 4));
                return res;
            }
        }

        public unsafe double ReadDouble()
        {
            if (State.ShouldReverseEndian)
            {
                long res = 0;
                ReadBytes(new Span<byte>((byte*)&res, 8));
                return BitConverter.Int64BitsToDouble(BinaryPrimitives.ReverseEndianness(res));
            }
            else
            {
                double res = 0;
                ReadBytes(new Span<byte>((byte*)&res, 8));
                return res;
            }
        }

        public decimal ReadDecimal()
        {
            // TODO: Optimize this.
            int[]? bits = new int[4];

            for (int i = 0; i < 4; i++)
                bits[i] = ReadInt32();

            return new decimal(bits);
        }

        public unsafe void FastReadShorts(Span<short> dest)
        {
            Span<byte> destBytes = MemoryMarshal.Cast<short, byte>(dest);

            if (State.ShouldReverseEndian)
            {
                // TODO: Optimize?
                byte* buffer = stackalloc byte[2];
                var bufferSpan = new Span<byte>(buffer, 2);

                int i = 0;
                while (i < destBytes.Length)
                {
                    ReadBytes(bufferSpan);

                    destBytes[i++] = buffer[1];
                    destBytes[i++] = buffer[0];
                }
            }
            else ReadBytes(destBytes);
        }

        #endregion

        public void Initialize(Stream source, bool? writeVersioning)
        {
            Source = source;
            Reset();

            if (writeVersioning == null)
            {
                if (!State.Settings.IncludeVersioningHeader)
                    throw new Exception("Because 'IncludeVersioningHeader' is disabled in the settings, ABSave cannot automatically discover whether to include versioning numbers are present while deserializing. You must provide 'writeVersioning' to deserialization too, so it can know whether it's present or not.");
            }
            else
            {
                if (State.Settings.IncludeVersioningHeader)
                    throw new Exception("When DEserializing, the field 'writeVersioning' should be left blank unless 'IncludeVersioningHeader' is disabled in the settings, because when enabled the versioning header is how ABSave determines whether version numbers are present or not when deserializing.");

                State.HasVersioningInfo = writeVersioning.Value;
            }
        }

        public BitReader GetHeader()
        {
            _currentBitReader.SetupHeader();
            return _currentBitReader;
        }
    }
}
