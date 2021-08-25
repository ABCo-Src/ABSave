using ABCo.ABSave.Configuration;
using ABCo.ABSave.Serialization.Converters;
using ABCo.ABSave.Serialization.Reading;
using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description;
using ABCo.ABSave.Mapping.Description.Attributes;
using ABCo.ABSave.Mapping.Generation.Inheritance;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using ABCo.ABSave.Serialization.Writing.Core;
using System.Buffers;

namespace ABCo.ABSave.Serialization.Writing
{
    /// <summary>
    /// The central object that everything in ABSave writes to. Provides facilties to write primitive types, including strings.
    /// </summary>
    public sealed partial class ABSaveSerializer : IDisposable
    {
        Stream _output = null!;
        public SerializeCurrentState State { get; }

        public byte CurrentByteFreeBits => _currentBitWriter.FreeBits;

        BitWriter _currentBitWriter;

        internal ABSaveSerializer(ABSaveMap map)
        {
            _output = null!;
            State = new SerializeCurrentState(map);
            _currentBitWriter = new BitWriter(this);
        }

        public void Initialize(Stream output, bool writeVersioning, Dictionary<Type, uint>? targetVersions)
        {
            if (!output.CanWrite)
                throw new Exception("Cannot use unwriteable stream.");

            _output = output;
            State.TargetVersions = targetVersions;
            State.HasVersioningInfo = writeVersioning;

            Reset();
        }

        public void Reset()
        {
            _currentBitWriter.Reset();
            State.Reset();
        }

        public void Dispose()
        {
            Flush();
            State.Map.ReleaseSerializer(this);
        }

        public Stream GetStream()
        {
            _currentBitWriter.Finish();
            return _output;
        }

        #region Bit Writing

        public void WriteBitOn() => _currentBitWriter.WriteBitOn();
        public void WriteBitOff() => _currentBitWriter.WriteBitOff();
        public void WriteBitWith(bool value) => _currentBitWriter.WriteBitWith(value);
        public void WriteInteger(byte number, byte bitsRequired) => _currentBitWriter.WriteInteger(number, bitsRequired);
        public void FillRemainderOfCurrentByteWith(int n) => _currentBitWriter.FillRemainingWith(n);
        public void FinishWritingBitsToCurrentByte() => _currentBitWriter.MoveToNextByte();

        #endregion

        #region Byte Writing

        internal void WriteByteUnchecked(byte byt) => _output.WriteByte(byt);

        public void WriteByte(byte byt)
        {
            _currentBitWriter.Finish();
            _output.WriteByte(byt);
        }

        public void WriteRawBytes(byte[] arr)
        {
            _currentBitWriter.Finish();
            _output.Write(arr, 0, arr.Length);
        }

        public void WriteRawBytes(ReadOnlySpan<byte> data) 
        {
            _currentBitWriter.Finish();

#if NETSTANDARD2_0
            byte[] buffer = ArrayPool<byte>.Shared.Rent(data.Length);
            try
            {
                data.CopyTo(buffer);
                _output.Write(buffer, 0, data.Length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
#else
            _output.Write(data);
#endif
        }

        #endregion

        #region Numerical Writing

        /// <summary>
        /// Writes the raw bytes of the given short, to the endianness selected in the settings. This will always write two bytes.
        /// </summary>
        public unsafe void WriteInt16(short num)
        {
            if (State.ShouldReverseEndian) num = BinaryPrimitives.ReverseEndianness(num);
            WriteRawBytes(new ReadOnlySpan<byte>((byte*)&num, 2));
        }

        /// <summary>
        /// Writes the raw bytes of the given int, to the endianness selected in the settings. This will always write four bytes.
        /// </summary>
        public unsafe void WriteInt32(int num)
        {
            if (State.ShouldReverseEndian) num = BinaryPrimitives.ReverseEndianness(num);
            WriteRawBytes(new ReadOnlySpan<byte>((byte*)&num, 4));
        }

        /// <summary>
        /// Writes the raw bytes of the given long, to the endianness selected in the settings. This will always write eight bytes.
        /// </summary>
        public unsafe void WriteInt64(long num)
        {
            if (State.ShouldReverseEndian) num = BinaryPrimitives.ReverseEndianness(num);
            WriteRawBytes(new ReadOnlySpan<byte>((byte*)&num, 8));
        }

        /// <summary>
        /// Writes the raw bytes of the given float, to the endianness selected in the settings. This will always write four bytes.
        /// </summary>
        public unsafe void WriteSingle(float num)
        {
            if (State.ShouldReverseEndian)
            {
#if NETSTANDARD2_0
                int asInt = *(int*)&num;
#else
                int asInt = BitConverter.SingleToInt32Bits(num);
#endif
                asInt = BinaryPrimitives.ReverseEndianness(asInt);
                WriteRawBytes(new ReadOnlySpan<byte>((byte*)&asInt, 4));
            }
            else WriteRawBytes(new ReadOnlySpan<byte>((byte*)&num, 4));
        }

        /// <summary>
        /// Writes the raw bytes of the given double, to the endianness selected in the settings. This will always write eight bytes.
        /// </summary>
        public unsafe void WriteDouble(double num)
        {
            if (State.ShouldReverseEndian)
            {
                long asInt = BitConverter.DoubleToInt64Bits(num);
                asInt = BinaryPrimitives.ReverseEndianness(asInt);
                WriteRawBytes(new ReadOnlySpan<byte>((byte*)&asInt, 8));
            }
            else WriteRawBytes(new ReadOnlySpan<byte>((byte*)&num, 8));
        }

        /// <summary>
        /// Writes the four int "bits" of the given decimal, each individually to the endianness selected in the settings. This will always write sixteen bytes.
        /// </summary>
        public void WriteDecimal(decimal num)
        {
            int[]? bits = decimal.GetBits(num);
            for (int i = 0; i < 4; i++)
                WriteInt32(bits[i]);
        }

        /// <summary>
        /// Writes an array of shorts as quickly as possible, considering the endianness of each item.
        /// </summary>
        public unsafe void FastWriteShorts(ReadOnlySpan<short> shorts)
        {
            ReadOnlySpan<byte> bytes = MemoryMarshal.Cast<short, byte>(shorts);

            if (State.ShouldReverseEndian)
            {
                // TODO: Optimize?
                byte* buffer = stackalloc byte[2];
                var bufferSpan = new ReadOnlySpan<byte>(buffer, 2);

                int i = 0;
                while (i < bytes.Length)
                {
                    buffer[1] = bytes[i++];
                    buffer[0] = bytes[i++];

                    WriteRawBytes(bufferSpan);
                }
            }
            else WriteRawBytes(bytes);
        }

        public void Flush() => _currentBitWriter.Finish();

#endregion

        public void WriteRoot(object? obj) => ItemSerializer.SerializeItem(obj, State.Map._rootItem, this);

        public void WriteSettingsHeaderIfNeeded() => HeaderSerializer.WriteHeader(this);

        public void WriteItem(object? obj, MapItemInfo info) => ItemSerializer.SerializeItem(obj, info, this);
        public void WriteExactNonNullItem(object obj, MapItemInfo info) => ItemSerializer.SerializeExactNonNullItem(obj, info, this);

        public VersionInfo WriteExactNonNullHeader(object obj, Type actualType, Converter converter) =>
            ItemSerializer.SerializeConverterHeader(obj, converter, actualType, true, this)!;

        public void WriteCompressedInt(uint data) => CompressedSerializer.WriteCompressedInt(data, this);
        public void WriteCompressedLong(ulong data) => CompressedSerializer.WriteCompressedLong(data, this);
        public void WriteNullableString(string? str) => TextSerializer.WriteString(str, this);
        public void WriteNonNullString(string str) => TextSerializer.WriteNonNullString(str, this);

        public void WriteText(ReadOnlySpan<char> bytes) => TextSerializer.WriteText(bytes, this);
        public void WriteUTF8(ReadOnlySpan<char> bytes) => TextSerializer.WriteUTF8(bytes, this);
    }
}
