using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Serialization;

namespace ABSoftware.ABSave
{
    public sealed class ABSaveWriter
    {
        public ABSaveSettings Settings;
        internal Dictionary<Assembly, int> CachedAssemblies = new Dictionary<Assembly, int>();
        internal Dictionary<Type, int> CachedTypes = new Dictionary<Type, int>();

        public Stream Output;
        public bool ShouldReverseEndian;

        byte[] _stringBuffer;

        public ABSaveWriter(Stream writeTo, ABSaveSettings settings) 
        {
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

        #region Character & Short Writing

        unsafe void WriteUTF16(ushort* str, int strLength)
        {
            WriteInt32((uint)strLength);
            FastWriteShorts(str, strLength);
        }

        public unsafe void FastWriteShorts(ushort* shorts, int shortsLength)
        {
            if (ShouldReverseEndian)
            {
                byte* buffer = stackalloc byte[2];
                byte* strData = (byte*)shorts;

                var bufferSpan = new ReadOnlySpan<byte>(buffer, 2);

                for (int i = 0; i < shortsLength; i++)
                {
                    buffer[1] = *strData++;
                    buffer[0] = *strData++;

                    Output.Write(bufferSpan);
                }
            }
            else Output.Write(new ReadOnlySpan<byte>((byte*)shorts, shortsLength * 2));
        }

        unsafe void WriteNullTerminatedUTF8(ushort* data, int length)
        {
            int maxSize = Encoding.UTF8.GetMaxByteCount(length);
            Span<byte> buffer = maxSize <= 128 ? stackalloc byte[maxSize] : GetStringBufferFor(maxSize);

            int actualLength = EncodeUTF8ToStringBuffer(data, buffer, length);

            // Copy the encoded string in, while escaping null characters using overlong sequences.
            fixed (byte* bufferData = buffer)
                FastWriteWithNullTermination(bufferData, actualLength);

            Output.WriteByte(0);
        }

        unsafe void WriteUTF8(ushort* data, int length)
        {
            WriteInt32((uint)length);

            int maxSize = Encoding.UTF8.GetMaxByteCount(length);
            Span<byte> buffer = maxSize <= 128 ? stackalloc byte[maxSize] : GetStringBufferFor(maxSize);

            int actualLength = EncodeUTF8ToStringBuffer(data, buffer, length);
            Output.Write(buffer.Slice(0, actualLength));
        }

        private unsafe int EncodeUTF8ToStringBuffer(ushort* data, Span<byte> buffer, int length) => Encoding.UTF8.GetBytes(new ReadOnlySpan<char>((char*)data, length), buffer);

        byte[] GetStringBufferFor(int length)
        {
            if (_stringBuffer == null || _stringBuffer.Length < length) return _stringBuffer = new byte[length];
            else return _stringBuffer;
        }

        unsafe void FastWriteWithNullTermination(byte* byteData, int sourceLength)
        {
            // Copy 4 bytes at a time.
            // NOTE: There are reasons why we aren't working with 8 bytes. Not only would the library have to be re-compiled for every platform on release (unless we do a runtime check for 64-bit),
            // but when there are null characters, checking 8 bytes can actually be slower, as it has to manually iterate over all bytes in that much bigger 8-byte chunk
            // up towards that null character. That being said, it's possible that null handling can be optimized (it checks in smaller chunks 
            // if it sees a 8-byte chunk contains a null byte), so it might be worth looking into if anyone has the time!
            while (sourceLength >= 4)
            {
                if (ABSaveUtils.ContainsZeroByte(*(uint*)byteData))
                {
                    // Write all the non-null characters.
                    while (*byteData != 0)
                    {
                        Output.WriteByte(*byteData++);
                        sourceLength--;
                    }

                    WriteOverlongNullSequence();
                    byteData++;
                    sourceLength--;
                }

                else
                {
                    Output.Write(new ReadOnlySpan<byte>(byteData, 4));
                    byteData += 4;
                    sourceLength -= 4;
                }
            }

            // Copy the rest manually.
            for (int i = 0; i < sourceLength; i++)
            {
                if (*byteData == 0)
                    WriteOverlongNullSequence();
                else
                    Output.WriteByte(*byteData);

                byteData++;
            }
        }

        private unsafe void WriteOverlongNullSequence()
        {
            Output.WriteByte(0xC0);
            Output.WriteByte(0x80);
        }

        // Using the built-in encoder is faster, even though it requires an extra copy.
        //unsafe ushort* WriteUTF8Char(ushort* data)
        //{
        //    ushort lowSurrogate = *data++;

        //    // UTF-8: 7-bits
        //    if (lowSurrogate < 0x7F)
        //        WriteByte((byte)lowSurrogate);

        //    // UTF-8: 11-bits
        //    else if (lowSurrogate < 2048)
        //    {
        //        WriteByte((byte)(0xC0 | (lowSurrogate >> 6)));
        //        WriteByte((byte)(0x80 | (lowSurrogate & 0b111111)));
        //    }

        //    // UTF-8: 16-bits
        //    else if (lowSurrogate < 0xD800 || lowSurrogate >= 0xE000)
        //    {
        //        WriteByte((byte)(0xE0 | (lowSurrogate >> 12)));
        //        WriteByte((byte)(0x80 | ((lowSurrogate >> 6) & 0b111111)));
        //        WriteByte((byte)(0x80 | (lowSurrogate & 0b111111)));
        //    }

        //    // UTF-8: 20-bits
        //    else
        //    {
        //        // 2 code points required
        //        ushort highSurrogate = *data++;
        //        WriteByte((byte)(0xF0 | ((lowSurrogate >> 8) & 0b11)));
        //        WriteByte((byte)(0x80 | ((lowSurrogate >> 2) & 0b111111)));
        //        WriteByte((byte)(0x80 | ((lowSurrogate & 0b11) << 4) | (highSurrogate >> 6)));
        //        WriteByte((byte)(0x80 | (highSurrogate & 0b111111)));
        //    }

        //    return data;
        //}

        unsafe void WriteText(ushort* txt, int length)
        {
            switch (Settings.TextMode)
            {
                case TextMode.UTF8:
                    WriteUTF8(txt, length);
                    break;
                case TextMode.NullTerminatedUTF8:
                    WriteNullTerminatedUTF8(txt, length);
                    break;
                case TextMode.UTF16:
                    WriteUTF16(txt, length);
                    break;
                default: throw new Exception("Invalid TextMode provided");
            }
        }

        public unsafe void WriteString(string str)
        {
            fixed (char* s = str)
                WriteText((ushort*)s, str.Length);
        }

        public unsafe void WriteCharArray(char[] chArr)
        {
            fixed (char* s = chArr)
                WriteText((ushort*)s, chArr.Length);
        }

        public void WriteStringBuilder(StringBuilder str)
        {
            char[] builderContents = new char[str.Length];
            str.CopyTo(0, builderContents, 0, str.Length);
            WriteCharArray(builderContents);
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

        public unsafe void WriteLittleEndianInt32(uint s, int significantBytes)
        {
            byte* data = (byte*)&s;

            // L ++-- --++ B
            if (BitConverter.IsLittleEndian)
                Output.Write(new ReadOnlySpan<byte>(data, significantBytes));
            else
            {
                byte* dest = stackalloc byte[significantBytes];
                byte* destPos = dest;

                data += significantBytes;
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
                    default:
                        throw new Exception("Invalid numerical type. Are you sure you have the right converter for the right item in your map?");
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
