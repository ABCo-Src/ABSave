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
    public sealed partial class ABSaveDeserializer : IDisposable
    {
        Stream _source;
        public DeserializeCurrentState State { get; private set; }

        public byte CurrentByteFreeBits => _currentBitReader.FreeBits;

        BitReader _currentBitReader;

        internal ABSaveDeserializer(ABSaveMap map)
        {
            _source = null!;
            State = new DeserializeCurrentState(map);
            _currentBitReader = new BitReader(this);
        }

        public void Reset()
        {
            State.Reset();
            State.CachedKeys.Clear();
        }

        public void Dispose() => State.Map.ReleaseDeserializer(this);

        public void ReadSettingsHeaderIfNeeded() => HeaderDeserializer.ReadHeader(this);

        public uint ReadCompressedInt() => (uint)CompressedDeserializer.ReadCompressed(false, this);
        public ulong ReadCompressedLong() => CompressedDeserializer.ReadCompressed(true, this);

        public string? ReadNullableString() => TextDeserializer.ReadNullableString(this);
        public string ReadNonNullString() => TextDeserializer.ReadNonNullString(this);
        public T ReadUTF8<T>(Func<int, T> createDest, Func<T, Memory<char>> castDest) => TextDeserializer.ReadUTF8<T>(createDest, castDest, this);

        public object? ReadRoot() => ItemDeserializer.DeserializeItem(State.Map._rootItem, this);
        public object? ReadItem(Converter info) => ItemDeserializer.DeserializeItem(info, this);
        public object ReadExactNonNullItem(Converter info) => ItemDeserializer.DeserializeExactNonNullItem(info, this);

        public VersionInfo ReadExactNonNullHeader(Converter converter)
        {
            ItemDeserializer.DeserializeConverterHeader(converter, this, true, out var info);
            return info;
        }

        #region Bit Reading

        public bool ReadBit() => _currentBitReader.ReadBit();
        public byte ReadInteger(byte bitsRequired) => _currentBitReader.ReadInteger(bitsRequired);
        public byte GetCurrentByte() => _currentBitReader.CurrentByte;
        public void MoveToNewCurrentByte() => _currentBitReader.MoveToNewByte();

        #endregion

        #region Byte Reading

        public byte ReadByte()
        {
            _currentBitReader.Reset();
            return (byte)_source.ReadByte();
        }

        public void ReadBytes(Span<byte> dest)
        {
            _currentBitReader.Reset();

#if NETSTANDARD2_0
            byte[] buffer = ArrayPool<byte>.Shared.Rent(dest.Length);
            try
            {
                _source.Read(buffer, 0, dest.Length);
                buffer.AsSpan().Slice(0, dest.Length).CopyTo(dest);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
#else
            _source.Read(dest);
#endif
        }

        public void ReadBytes(byte[] dest)
        {
            _currentBitReader.Reset();
            _source.Read(dest, 0, dest.Length);
        }

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

        public Stream GetStream() => _source;

        public void Initialize(Stream source, bool? writeVersioning)
        {
            _source = source;
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
    }
}
