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
using System.Buffers;

namespace ABCo.ABSave.Serialization.Reading
{
    public sealed class ABSaveDeserializer : IDisposable
    {
        Stream _rawSourceStream;
        public DeserializeCurrentState State { get; private set; }

        public byte CurrentByteFreeBits => _currentBitReader.FreeBits;

        BitReader _currentBitReader;

        internal ABSaveDeserializer(ABSaveMap map)
        {
            _rawSourceStream = null!;
            State = new DeserializeCurrentState(map);
            _currentBitReader = new BitReader(this);
        }

        public void Reset()
        {
            State.Reset();
            State.CachedKeys.Clear();
            _currentBitReader.Reset();
        }

        public void Dispose() => State.Map.ReleaseDeserializer(this);

        public void ReadSettingsHeaderIfNeeded() => HeaderDeserializer.ReadHeader(this);

        public uint ReadCompressedIntSigned() => (uint)CompressedDeserializer.ReadCompressedSigned<uint>(this);
        public ulong ReadCompressedLongSigned() => CompressedDeserializer.ReadCompressedSigned<ulong>(this);

        public uint ReadCompressedInt() => (uint)CompressedDeserializer.ReadCompressed<uint>(this);
        public ulong ReadCompressedLong() => CompressedDeserializer.ReadCompressed<ulong>(this);

        public string? ReadNullableString() => TextDeserializer.ReadNullableString(this);
        public string ReadNonNullString() => TextDeserializer.ReadNonNullString(this);
        public T ReadUTF8<T>(Func<int, T> createDest, Func<T, Memory<char>> castDest) => TextDeserializer.ReadUTF8<T>(createDest, castDest, this);

        public object? ReadRoot() => ItemDeserializer.DeserializeItem(State.Map._rootItem, this);
        public object? ReadItem(MapItemInfo info) => ItemDeserializer.DeserializeItem(info, this);
        public object ReadExactNonNullItem(MapItemInfo info) => ItemDeserializer.DeserializeExactNonNullItem(info, this);

        public VersionInfo ReadVersionInfo(Converter converter) => ItemDeserializer.DeserializeVersionInfo(converter, this);

        #region Bit Reading

        public bool ReadBit() => _currentBitReader.ReadBit();
        public byte ReadInteger(byte bitsRequired) => _currentBitReader.ReadInteger(bitsRequired);
        public byte GetCurrentByte() => _currentBitReader.CurrentByte;
        public byte ReadRestOfCurrentByte() => _currentBitReader.ReadInteger(_currentBitReader.FreeBits);
        public void SkipRestOfCurrentByte() => _currentBitReader.SkipRestOfCurrentByte();

        #endregion

        #region Byte Reading

        public byte ReadByte() => (byte)GetStream().ReadByte();        

        public void ReadBytes(Span<byte> dest)
        {
#if NETSTANDARD2_0
            byte[] buffer = ArrayPool<byte>.Shared.Rent(dest.Length);
            try
            {
                GetStream().Read(buffer, 0, dest.Length);
                buffer.AsSpan().Slice(0, dest.Length).CopyTo(dest);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
#else
            GetStream().Read(dest);
#endif
        }

        public void ReadBytes(byte[] dest) => GetStream().Read(dest, 0, dest.Length);

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

                int reversed = BinaryPrimitives.ReverseEndianness(res);

#if NETSTANDARD2_0
                return *((float*)&reversed);
#else
                return BitConverter.Int32BitsToSingle(reversed);
#endif
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

                long reversed = BinaryPrimitives.ReverseEndianness(res);

#if NETSTANDARD2_0
                return *((double*)&reversed);
#else
                return BitConverter.Int64BitsToDouble(reversed);
#endif
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

        public Stream GetStream()
        {
            _currentBitReader.Reset();
            return _rawSourceStream;
        }

        public void Initialize(Stream source, bool? writeVersioning)
        {
            _rawSourceStream = source;
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

                State.IncludeVersioningInfo = writeVersioning.Value;
            }
        }
    }
}
