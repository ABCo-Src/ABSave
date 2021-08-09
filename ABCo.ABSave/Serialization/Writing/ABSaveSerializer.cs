using ABCo.ABSave.Configuration;
using ABCo.ABSave.Serialization.Converters;
using ABCo.ABSave.Serialization.Writing.Reading;
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

        public void Initialize(Stream output, Dictionary<Type, uint>? targetVersions)
        {
            if (!output.CanWrite)
                throw new Exception("Cannot use unwriteable stream.");

            Output = output;
            State.TargetVersions = targetVersions;

            Reset();
        }

        public void Reset() => State.Reset();
        public void Dispose() => State.Map.ReleaseSerializer(this);

        public void SerializeRoot(object? obj) => WriteItem(obj, State.Map._rootItem);

        public void WriteItem(object? obj, MapItemInfo item)
        {
            using var writer = GetHeader();
            writer.WriteItem(obj, item);
        }

        public void WriteExactNonNullItem(object? obj, MapItemInfo item)
        {
            using var writer = GetHeader();
            writer.WriteExactNonNullItem(obj!, item);
        }

        #region Byte Writing

        public void WriteByte(byte byt) => Output.WriteByte(byt);
        public void WriteByteArray(byte[] arr) => Output.Write(arr, 0, arr.Length);
        public void WriteBytes(ReadOnlySpan<byte> data) => Output.Write(data);

        #endregion

        #region Numerical Writing

        public unsafe void WriteInt16(short num)
        {
            if (State.ShouldReverseEndian) num = BinaryPrimitives.ReverseEndianness(num);
            WriteBytes(new ReadOnlySpan<byte>((byte*)&num, 2));
        }

        public unsafe void WriteInt32(int num)
        {
            if (State.ShouldReverseEndian) num = BinaryPrimitives.ReverseEndianness(num);
            WriteBytes(new ReadOnlySpan<byte>((byte*)&num, 4));
        }

        public unsafe void WriteInt64(long num)
        {
            if (State.ShouldReverseEndian) num = BinaryPrimitives.ReverseEndianness(num);
            WriteBytes(new ReadOnlySpan<byte>((byte*)&num, 8));
        }

        public unsafe void WriteSingle(float num)
        {
            if (State.ShouldReverseEndian)
            {
                int asInt = BitConverter.SingleToInt32Bits(num);
                asInt = BinaryPrimitives.ReverseEndianness(asInt);
                WriteBytes(new ReadOnlySpan<byte>((byte*)&asInt, 4));
            }
            else WriteBytes(new ReadOnlySpan<byte>((byte*)&num, 4));
        }

        public unsafe void WriteDouble(double num)
        {
            if (State.ShouldReverseEndian)
            {
                long asInt = BitConverter.DoubleToInt64Bits(num);
                asInt = BinaryPrimitives.ReverseEndianness(asInt);
                WriteBytes(new ReadOnlySpan<byte>((byte*)&asInt, 8));
            }
            else WriteBytes(new ReadOnlySpan<byte>((byte*)&num, 8));
        }

        public void WriteDecimal(decimal num)
        {
            int[]? bits = decimal.GetBits(num);
            for (int i = 0; i < 4; i++)
                WriteInt32(bits[i]);
        }

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

                    WriteBytes(bufferSpan);
                }
            }
            else WriteBytes(bytes);
        }

        #endregion

        public void WriteNullableString(string? str)
        {
            using var writer = GetHeader();
            writer.WriteNullableString(str);
        }

        public void WriteNonNullString(string str)
        {
            using var writer = GetHeader();
            writer.WriteNonNullString(str);
        }

        public void WriteCompressedInt(uint data)
        {
            using var writer = GetHeader();
            writer.WriteCompressedInt(data);
        }

        public void WriteCompressedLong(ulong data)
        {
            using var writer = GetHeader();
            writer.WriteCompressedLong(data);
        }

        public BitWriter GetHeader()
        {
            _currentBitWriter.SetupHeader();
            return _currentBitWriter;
        }
    }
}
