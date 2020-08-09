using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;

namespace ABSoftware.ABSave.Serialization
{
    public sealed class ABSaveWriter
    {
        internal ABSaveSettings Settings;
        internal Dictionary<Assembly, int> CachedAssemblies = new Dictionary<Assembly, int>();
        internal Dictionary<Type, int> CachedTypes = new Dictionary<Type, int>();

        public Stream Output;

        public bool ShouldReverseEndian;

        public ABSaveWriter(Stream writeTo, ABSaveSettings settings) {
            if (!writeTo.CanWrite)
                throw new Exception("Cannot use unwriteable stream.");

            Output = writeTo;

            Settings = settings;
            ShouldReverseEndian = settings.UseLittleEndian != BitConverter.IsLittleEndian;
        }

        public void Reset()
        {
            CachedAssemblies.Clear();
            CachedTypes.Clear();
        }

        #region Byte Writing
        public void WriteByte(byte byt) => Output.WriteByte(byt);

        public void WriteByteArray(byte[] arr, bool writeSize)
        {
            if (writeSize) WriteInt32((uint)arr.Length);
            Output.Write(arr, 0, arr.Length);
        }

        public void WriteBytes(ReadOnlySpan<byte> data, bool writeSize)
        {
            if (writeSize) WriteInt32((uint)data.Length);
            Output.Write(data);
        }

        #endregion

        #region Short/Character Writing

        public unsafe void FastWriteShorts(short* str, int strLength)
        {
            int byteCount = strLength * 2;
            WriteInt32((uint)strLength);

            if (ShouldReverseEndian)
            {
                byte* dest = stackalloc byte[byteCount];

                byte* currentDestPos = dest;
                byte* strData = (byte*)str;

                for (int i = 0; i < strLength; i++)
                {
                    *currentDestPos++ = strData[1];
                    *currentDestPos++ = strData[0];
                    strData += 2;
                }

                Output.Write(new ReadOnlySpan<byte>(dest, byteCount));
            }
            else Output.Write(new ReadOnlySpan<byte>((byte*)str, byteCount));
        }

        public unsafe void WriteText(string str)
        {
            fixed (char* s = str)
                FastWriteShorts((short*)s, str.Length);
        }

        public unsafe void WriteText(char[] chArr)
        {
            fixed (char* s = chArr)
                FastWriteShorts((short*)s, chArr.Length);
        }

        public void WriteText(StringBuilder str)
        {
            char[] builderContents = new char[str.Length];
            str.CopyTo(0, builderContents, 0, str.Length);
            WriteText(builderContents);
        }

        #endregion

        #region Numerical Writing

        public unsafe void WriteInt16(ushort num)
        {
            byte* data = (byte*)&num;
            if (ShouldReverseEndian)
            {
                Output.WriteByte(data[1]);
                Output.WriteByte(data[0]);
            }
            else
            {
                Output.WriteByte(data[0]);
                Output.WriteByte(data[1]);
            }
        }

        public unsafe void WriteInt32(uint num) => NumericalWriteBytes((byte*)&num, 4);
        public unsafe void WriteInt64(ulong num) => NumericalWriteBytes((byte*)&num, 8);
        public unsafe void WriteSingle(float num) => NumericalWriteBytes((byte*)&num, 4);
        public unsafe void WriteDouble(double num) => NumericalWriteBytes((byte*)&num, 8);

        public void WriteDecimal(decimal num)
        {
            var bits = decimal.GetBits(num);
            for (int i = 0; i < 4; i++)
                WriteInt32((uint)bits[i]);
        }

        unsafe void NumericalWriteBytes(byte* data, int numberOfBytes)
        {
            if (ShouldReverseEndian)
            {
                byte* buffer = stackalloc byte[numberOfBytes];
                byte* bufferPos = buffer;

                byte* currentDataPos = data + numberOfBytes;
                for (int i = 0; i < numberOfBytes; i++)
                    *bufferPos++ = *--currentDataPos;

                Output.Write(new ReadOnlySpan<byte>(buffer, numberOfBytes));
            } 
            
            else Output.Write(new ReadOnlySpan<byte>(data, numberOfBytes));
        }

        public unsafe void WriteLittleEndianInt32(int s, int significantBytes)
        {
            byte* data = (byte*)&s;

            // L ++-- --++ B
            if (BitConverter.IsLittleEndian)
                Output.Write(new ReadOnlySpan<byte>(data, significantBytes));
            else
            {
                byte* dest = stackalloc byte[4];
                byte* destPos = dest;

                data += 4;
                for (int i = 0; i < significantBytes; i++)
                    *destPos++ = *--data;

                Output.Write(new ReadOnlySpan<byte>(dest, significantBytes));
            }
        }

        public unsafe void WriteNumber(object num, TypeCode tCode)
        {
            unchecked
            {
                switch (tCode)
                {
                    case TypeCode.Byte:

                        WriteByte((byte)num);
                        break;

                    case TypeCode.SByte:

                        WriteByte((byte)(sbyte)num);
                        break;

                    case TypeCode.UInt16:

                        WriteInt16((ushort)num);
                        break;

                    case TypeCode.Int16:

                        WriteInt16((ushort)(short)num);
                        break;

                    case TypeCode.Char:

                        WriteInt16((char)num);
                        break;

                    case TypeCode.UInt32:

                        WriteInt32((uint)num);
                        break;

                    case TypeCode.Int32:

                        WriteInt32((uint)(int)num);
                        break;

                    case TypeCode.UInt64:

                        WriteInt64((ulong)num);
                        break;

                    case TypeCode.Int64:

                        WriteInt64((ulong)(long)num);
                        break;

                    case TypeCode.Single:

                        WriteSingle((float)num);
                        break;

                    case TypeCode.Double:

                        WriteDouble((double)num);
                        break;

                    case TypeCode.Decimal:

                        WriteDecimal((decimal)num);
                        break;
                }
            }
        }

        #endregion

        #region Attributes
        public void WriteNullAttribute() => WriteByte(1);
        public void WriteMatchingTypeAttribute() => WriteByte(2);
        public void WriteDifferentTypeAttribute() => WriteByte(3);
        #endregion
    }
}
