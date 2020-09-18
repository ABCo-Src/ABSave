using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ABSoftware.ABSave
{
    public class ABSaveReader
    {
        internal ABSaveSettings Settings;
        internal List<Assembly> CachedAssemblies = new List<Assembly>();
        internal List<Type> CachedTypes = new List<Type>();

        public Stream Source;
        public bool ShouldReverseEndian;

        byte[] _stringBuffer;

        public ABSaveReader(Stream source, ABSaveSettings settings)
        {
            Settings = settings;
            ShouldReverseEndian = settings.UseLittleEndian != BitConverter.IsLittleEndian;

            Source = source;
        }

        public void Reset()
        {
            CachedAssemblies.Clear();
            CachedTypes.Clear();
        }

        #region Byte Reading

        public byte ReadByte() => (byte)Source.ReadByte();
        public void ReadBytes(Span<byte> dest) => Source.Read(dest);
        public void ReadBytes(byte[] dest) => Source.Read(dest, 0, dest.Length);

        #endregion

        #region Text Reading

        unsafe Span<byte> ReadNullTerminatedBytesIntoBuffer(byte* stackSpace)
        {
            int currentPos = 0;

            // We'll attempt to use the stack if the string fits into 128 bytes.
            bool bufferIsStack = true;
            Span<byte> buffer = new Span<byte>(stackSpace, 128);

        ReadData:
            // Read as many characters as we can fit into the buffer, unless we reach a null character.
            for (; currentPos < buffer.Length; currentPos++)
            {
                int currentByte = Source.ReadByte();

                switch (currentByte)
                {
                    case 0:
                        return bufferIsStack ? new Span<byte>(stackSpace, currentPos) : new Span<byte>(_stringBuffer, 0, currentPos);
                    case 0xC0:

                        int nextByte = Source.ReadByte();

                        if (nextByte == 0x80)
                            buffer[currentPos] = 0;
                        else
                        {
                            buffer[currentPos++] = 0xC0;
                            buffer[currentPos] = (byte)nextByte;
                        }

                        break;
                    default:
                        buffer[currentPos] = (byte)currentByte;
                        break;
                }
            }

            // The data didn't fit into whatever buffer we had, so we need to (re)allocate onto the heap and continue reading into that.
            if (bufferIsStack)
            {
                buffer = GetStringBufferFor(256);

                fixed (byte* heapData = _stringBuffer)
                    Buffer.MemoryCopy(stackSpace, heapData, 256, 128);

                bufferIsStack = false;
            }
            else Array.Resize(ref _stringBuffer, _stringBuffer.Length * 2);

            buffer = _stringBuffer;
            goto ReadData;
        }

        public unsafe void FastReadShorts(ushort* dest, uint size)
        {
            var byteSize = size * 2;
            var destData = (byte*)dest;

            if (ShouldReverseEndian)
            {
                byte* buffer = stackalloc byte[2];
                byte* strData = (byte*)dest;

                var bufferSpan = new Span<byte>(buffer, 2);

                for (int i = 0; i < size; i++)
                {
                    Source.Read(bufferSpan);

                    *strData++ = buffer[1];
                    *strData++ = buffer[0];
                }

            }
            else Source.Read(new Span<byte>(destData, (int)byteSize));
        }

        public unsafe string ReadString()
        {
            switch (Settings.TextMode)
            {
                case TextMode.UTF8:
                    int size = (int)ReadInt32();

                    Span<byte> buffer = size <= 128 ? stackalloc byte[size] : GetStringBufferFor(size);
                    Source.Read(buffer);

                    return Encoding.UTF8.GetString(buffer);
                case TextMode.NullTerminatedUTF8:
                    byte* stackSpace = stackalloc byte[128];

                    Span<byte> nullTerminatedBuffer = ReadNullTerminatedBytesIntoBuffer(stackSpace);
                    return Encoding.UTF8.GetString(nullTerminatedBuffer);
                case TextMode.UTF16:
                    uint utf16Size = ReadInt32();
                    var str = new string('\0', (int)utf16Size);

                    fixed (char* strData = str)
                        FastReadShorts((ushort*)strData, utf16Size);

                    return str;
                default: throw new Exception("Invalid TextMode provided");
            }
        }

        public unsafe char[] ReadCharArr()
        {
            switch (Settings.TextMode)
            {
                case TextMode.UTF8:
                    int size = (int)ReadInt32();

                    Span<byte> utf8Buffer = size <= 128 ? stackalloc byte[size] : GetStringBufferFor(size);
                    Source.Read(utf8Buffer);

                    return GetCharArrFromBuffer(utf8Buffer);
                case TextMode.NullTerminatedUTF8:

                    byte* stackSpace = stackalloc byte[128];
                    Span<byte> nullTerminatedBuffer = ReadNullTerminatedBytesIntoBuffer(stackSpace);

                    return GetCharArrFromBuffer(nullTerminatedBuffer);
                case TextMode.UTF16:
                    uint utf16Size = ReadInt32();
                    var utf16Res = new char[(int)utf16Size];

                    fixed (char* resData = utf16Res)
                        FastReadShorts((ushort*)resData, utf16Size);

                    return utf16Res;
                default: throw new Exception("Invalid TextMode provided");
            }
        }

        static char[] GetCharArrFromBuffer(Span<byte> utf8Buffer)
        {
            var res = new char[Encoding.UTF8.GetCharCount(utf8Buffer)];
            Encoding.UTF8.GetChars(utf8Buffer, new Span<char>(res));

            return res;
        }

        public unsafe StringBuilder ReadStringBuilder()
        {
            var size = ReadInt32();
            var buffer = stackalloc char[(int)size];

            FastReadShorts((ushort*)buffer, size);

            var sb = new StringBuilder((int)size);
            sb.Append(buffer, (int)size);
            return sb;
        }

        byte[] GetStringBufferFor(int length)
        {
            if (_stringBuffer == null || _stringBuffer.Length < length) return _stringBuffer = new byte[length];
            else return _stringBuffer;
        }

        #endregion

        #region Numerical Reading

        public unsafe ushort ReadInt16()
        {
            ushort res = 0;
            NumericalReadBytes((byte*)&res, 2);
            return res;
        }

        public unsafe uint ReadInt32()
        {
            uint data = 0;
            NumericalReadBytes((byte*)&data, 4);
            return data;
        }

        public unsafe ulong ReadInt64()
        {
            ulong data = 0;
            NumericalReadBytes((byte*)&data, 8);
            return data;
        }

        public unsafe float ReadSingle()
        {
            float data = 0;
            NumericalReadBytes((byte*)&data, 4);
            return data;
        }

        public unsafe double ReadDouble()
        {
            double data = 0;
            NumericalReadBytes((byte*)&data, 8);
            return data;
        }

        public decimal ReadDecimal()
        {
            // TODO: Optimize this.
            var bits = new int[4];

            for (int i = 0; i < 4; i++)
                bits[i] = (int)ReadInt32();

            return new decimal(bits);
        }

        unsafe void NumericalReadBytes(byte* data, int numberOfBytes)
        {
            if (ShouldReverseEndian)
            {
                byte* buffer = stackalloc byte[numberOfBytes];
                Source.Read(new Span<byte>(buffer, numberOfBytes));

                buffer += numberOfBytes;

                for (int i = 0; i < numberOfBytes; i++)
                    *data++ = *--buffer;
            }

            else Source.Read(new Span<byte>(data, numberOfBytes));
        }

        public unsafe uint ReadLittleEndianInt32(int significantBytes)
        {
            uint dest = 0;
            byte* destPos = (byte*)&dest;

            if (BitConverter.IsLittleEndian)
                Source.Read(new Span<byte>(destPos, significantBytes));
            else
            {
                byte* src = stackalloc byte[significantBytes];
                Source.Read(new Span<byte>(src, significantBytes));

                destPos += significantBytes;
                for (int i = 0; i < significantBytes; i++)
                    *--destPos = *src++;
            }

            return dest;
        }

        #endregion

        public unsafe object ReadNumber(TypeCode tCode)
        {
            unchecked
            {
                return tCode switch
                {
                    TypeCode.Byte => ReadByte(),
                    TypeCode.SByte => (sbyte)ReadByte(),
                    TypeCode.UInt16 => ReadInt16(),
                    TypeCode.Int16 => (short)ReadInt16(),
                    TypeCode.Char => (char)ReadInt16(),
                    TypeCode.UInt32 => ReadInt32(),
                    TypeCode.Int32 => (int)ReadInt32(),
                    TypeCode.UInt64 => ReadInt64(),
                    TypeCode.Int64 => (long)ReadInt64(),
                    TypeCode.Single => ReadSingle(),
                    TypeCode.Double => ReadDouble(),
                    TypeCode.Decimal => ReadDecimal(),
                    _ => throw new Exception("Invalid numerical type. Are you sure you have the right converter for the right item in your map?"),
                };
            }
        }
    }
}
