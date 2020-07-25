using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave.Serialization.Writer
{
    /// <summary>
    /// The central part of ABSave serialization, this contains information about the state of the ABSave document and the actual serialized data of the document.
    /// There is a memory writer and a stream writer implementing this class.
    /// </summary>
    public abstract class ABSaveWriter
    {
        internal ABSaveSettings Settings;
        internal Dictionary<Assembly, int> CachedAssemblies = new Dictionary<Assembly, int>();
        internal Dictionary<Type, int> CachedTypes = new Dictionary<Type, int>();

        // Whether number's or character's endian should be reversed to match the target ABSave.
        public bool ShouldReverseEndian;

        public ABSaveWriter(ABSaveSettings settings)
        {
            Settings = settings;
            ShouldReverseEndian = settings.UseLittleEndian != BitConverter.IsLittleEndian;
        }

        public abstract void WriteByte(byte value);
        public abstract void WriteByteArray(byte[] arr, bool writeSize);
        public abstract unsafe void FastWriteShorts(short* str, int strLength);
        public abstract unsafe void WriteInt16(ushort num);
        public abstract unsafe void WriteInt32(uint num);
        public abstract unsafe void WriteInt64(ulong num);
        public abstract unsafe void WriteSingle(float num);
        public abstract unsafe void WriteDouble(double num);
        public abstract unsafe void WriteDecimal(decimal num);
        public abstract unsafe void WriteInt32ToSignificantBytes(int s, int significantBytes);

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

        public void WriteNullAttribute() => WriteByte(1);
        public void WriteMatchingTypeAttribute() => WriteByte(2);
        public void WriteDifferentTypeAttribute() => WriteByte(3);
    }
}
