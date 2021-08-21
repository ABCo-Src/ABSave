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

namespace ABCo.ABSave.Serialization.Writing
{
    /// <summary>
    /// The central object that everything in ABSave writes to. Provides facilties to write primitive types, including strings.
    /// </summary>
    public sealed partial class ABSaveSerializer : IDisposable
    {
        public Stream Output { get; private set; } = null!;
        public SerializeCurrentState State { get; }

        readonly BitWriter _currentBitWriter;

        internal ABSaveSerializer(ABSaveMap map)
        {
            Output = null!;
            State = new SerializeCurrentState(map);
            _currentBitWriter = new BitWriter(this);
        }

        public void Initialize(Stream output, bool writeVersioning, Dictionary<Type, uint>? targetVersions)
        {
            if (!output.CanWrite)
                throw new Exception("Cannot use unwriteable stream.");

            Output = output;
            State.TargetVersions = targetVersions;
            State.HasVersioningInfo = writeVersioning;

            Reset();
        }

        public void Reset()
        {
            _currentBitWriter.Reset();
            State.Reset();
        }

        public void Dispose() => State.Map.ReleaseSerializer(this);

        #region Bit Writing

        public void WriteBitOn() => _currentBitWriter.WriteBitOn();
        public void WriteBitOff() => _currentBitWriter.WriteBitOff();
        public void WriteBitWith(bool value) => _currentBitWriter.WriteBitWith(value);


        #endregion

        #region Byte Writing

        public void WriteByte(byte byt) => Output.WriteByte(byt);
        public void WriteRawBytes(byte[] arr) => Output.Write(arr, 0, arr.Length);
        public void WriteRawBytes(ReadOnlySpan<byte> data) => Output.Write(data);

        #endregion

        #region Numerical Writing

        /// <summary>
        /// Writes the raw bytes of the given short, to the endianness selected in the settings. This will always write two bytes.
        /// </summary>
        public unsafe void WriteInt16(short num)
        {
            _currentBitWriter.Finish();

            if (State.ShouldReverseEndian) num = BinaryPrimitives.ReverseEndianness(num);
            WriteRawBytes(new ReadOnlySpan<byte>((byte*)&num, 2));
        }

        /// <summary>
        /// Writes the raw bytes of the given int, to the endianness selected in the settings. This will always write four bytes.
        /// </summary>
        public unsafe void WriteInt32(int num)
        {
            _currentBitWriter.Finish();

            if (State.ShouldReverseEndian) num = BinaryPrimitives.ReverseEndianness(num);
            WriteRawBytes(new ReadOnlySpan<byte>((byte*)&num, 4));
        }

        /// <summary>
        /// Writes the raw bytes of the given long, to the endianness selected in the settings. This will always write eight bytes.
        /// </summary>
        public unsafe void WriteInt64(long num)
        {
            _currentBitWriter.Finish();

            if (State.ShouldReverseEndian) num = BinaryPrimitives.ReverseEndianness(num);
            WriteRawBytes(new ReadOnlySpan<byte>((byte*)&num, 8));
        }

        /// <summary>
        /// Writes the raw bytes of the given float, to the endianness selected in the settings. This will always write four bytes.
        /// </summary>
        public unsafe void WriteSingle(float num)
        {
            _currentBitWriter.Finish();

            if (State.ShouldReverseEndian)
            {
                int asInt = BitConverter.SingleToInt32Bits(num);
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
            _currentBitWriter.Finish();

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
            _currentBitWriter.Finish();

            int[]? bits = decimal.GetBits(num);
            for (int i = 0; i < 4; i++)
                WriteInt32(bits[i]);
        }

        /// <summary>
        /// Writes an array of shorts as quickly as possible, considering the endianness of each item.
        /// </summary>
        public unsafe void FastWriteShorts(ReadOnlySpan<short> shorts)
        {
            _currentBitWriter.Finish();

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

        public void WriteRoot(object? obj)
        {
            _currentBitWriter.WriteSettingsHeaderIfNeeded();
            ItemSerializer.SerializeItem(obj, State.Map._rootItem, _currentBitWriter);
        }

        public void WriteSettingsHeaderIfNeeded() => HeaderSerializer.WriteHeader(_currentBitWriter);

        public void WriteItem(object? obj, MapItemInfo info) => ItemSerializer.SerializeItem(obj, info, _currentBitWriter);
        public void WriteExactNonNullItem(object obj, MapItemInfo info) => ItemSerializer.SerializeExactNonNullItem(obj, info, _currentBitWriter);

        public void WriteCompressedInt(uint data) => CompressedSerializer.WriteCompressedInt(data, _currentBitWriter);
        public void WriteCompressedLong(ulong data) => CompressedSerializer.WriteCompressedLong(data, _currentBitWriter);
        public void WriteNullableString(string? str) => TextSerializer.WriteString(str, _currentBitWriter);
        public void WriteNonNullString(string str) => TextSerializer.WriteNonNullString(str, _currentBitWriter);

        public void WriteText(ReadOnlySpan<char> bytes) => TextSerializer.WriteText(bytes, _currentBitWriter);
        public void WriteUTF8(ReadOnlySpan<char> bytes) => TextSerializer.WriteUTF8(bytes, _currentBitWriter);

        public BitWriter GetHeader()
        {
            _currentBitWriter.Finish();
            return _currentBitWriter;
        }
    }
}
