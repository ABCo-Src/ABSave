using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ABSoftware.ABSave.Deserialization
{
    public class ABSaveReader
    {
        internal ABSaveSettings Settings;
        internal List<Assembly> CachedAssemblies = new List<Assembly>();
        internal List<Type> CachedTypes = new List<Type>();

        public Stream Source;
        public bool ShouldReverseEndian;

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

        public unsafe void FastReadShorts(short* dest, uint size)
        {
            var byteSize = size * 2;
            var destData = (byte*)dest;

            if (ShouldReverseEndian)
            {
                byte* buffer = stackalloc byte[(int)byteSize];
                Source.Read(new Span<byte>(buffer, (int)byteSize));

                byte* currentDestPos = destData;

                for (int i = 0; i < size; i++)
                {
                    *destData++ = buffer[1];
                    *destData++ = buffer[0];
                    buffer += 2;
                }

            }
            else Source.Read(new Span<byte>(destData, (int)byteSize));
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

            // L ++-- --++ B
            if (BitConverter.IsLittleEndian)
                Source.Read(new Span<byte>(destPos, 4));
            else
            {
                byte* src = stackalloc byte[4];
                Source.Read(new Span<byte>(src, 4));

                destPos += 4;
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
                    TypeCode.Int16 => (int)ReadInt16(),
                    TypeCode.Char => (char)ReadInt16(),
                    TypeCode.UInt32 => ReadInt32(),
                    TypeCode.Int32 => (int)ReadInt32(),
                    TypeCode.UInt64 => ReadInt64(),
                    TypeCode.Int64 => (int)ReadInt64(),
                    TypeCode.Single => ReadSingle(),
                    TypeCode.Double => ReadDouble(),
                    TypeCode.Decimal => ReadDecimal(),
                    _ => throw new Exception(),
                };
            }
        }

        public unsafe string ReadString()
        {
            var size = ReadInt32();
            var newStr = new string('\0', (int)size);

            fixed (char* ch = newStr)
                FastReadShorts((short*)ch, size);

            return newStr;
        }

        public unsafe char[] ReadCharArr()
        {
            var size = ReadInt32();
            var newStr = new char[(int)size];

            fixed (char* ch = newStr)
                FastReadShorts((short*)ch, size);

            return newStr;
        }

        public unsafe StringBuilder ReadStringBuilder()
        {
            var size = ReadInt32();
            var buffer = stackalloc char[(int)size];

            FastReadShorts((short*)buffer, size);

            var sb = new StringBuilder((int)size);
            sb.Append(buffer, (int)size);
            return sb;
        }
    }
}
