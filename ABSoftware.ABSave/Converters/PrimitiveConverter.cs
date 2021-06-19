using ABCo.ABSave.Deserialization;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Mapping.Generation;
using ABCo.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Converters
{
    [Select(typeof(bool))]
    [Select(typeof(byte))]
    [Select(typeof(sbyte))]
    [Select(typeof(char))]
    [Select(typeof(ushort))]
    [Select(typeof(short))]
    [Select(typeof(uint))]
    [Select(typeof(int))]
    [Select(typeof(ulong))]
    [Select(typeof(long))]
    [Select(typeof(float))]
    [Select(typeof(double))]
    [Select(typeof(decimal))]
    [Select(typeof(IntPtr))]
    [Select(typeof(UIntPtr))]
    public class PrimitiveConverter : Converter
    {
        PrimitiveType _typeCode;

        public override void Initialize(InitializeInfo info)
        {
            var typeCode = Type.GetTypeCode(info.Type);

            // IntPtr
            if (typeCode == TypeCode.Object)
                throw new Exception("Unsupported primitive provided. Please note that ABSave does not currently support .NET 5 and above types.");

            _typeCode = (PrimitiveType)typeCode;
        }

        public override bool CheckType(CheckTypeInfo info) => info.Type.IsPrimitive;

        public override void Serialize(in SerializeInfo info, ref BitTarget header)
        {
            var serializer = header.Serializer;

            switch (_typeCode)
            {
                case PrimitiveType.Boolean:
                    var bl = (bool)info.Instance;
                    if (bl) serializer.WriteByte(1);
                    else serializer.WriteByte(0);

                    break;

                //case PrimitiveType.IntPtr:

                //    if (IntPtr.Size == 8)
                //        serializer.WriteInt64((long)(IntPtr)obj);
                //    else
                //        serializer.WriteInt32((int)(IntPtr)obj);
                //    break;

                //case PrimitiveType.UIntPtr:

                //    if (UIntPtr.Size == 8)
                //        serializer.WriteInt64((long)(UIntPtr)obj);
                //    else
                //        serializer.WriteInt32((int)(UIntPtr)obj);
                //    break;

                case PrimitiveType.Byte:

                    serializer.WriteByte((byte)info.Instance);
                    break;

                case PrimitiveType.SByte:

                    serializer.WriteByte((byte)(sbyte)info.Instance);
                    break;

                case PrimitiveType.UInt16:

                    serializer.WriteInt16((short)(ushort)info.Instance);
                    break;

                case PrimitiveType.Int16:

                    serializer.WriteInt16((short)info.Instance);
                    break;

                case PrimitiveType.Char:

                    serializer.WriteInt16((short)(char)info.Instance);
                    break;

                case PrimitiveType.UInt32:

                    serializer.WriteInt32((int)(uint)info.Instance);
                    break;

                case PrimitiveType.Int32:

                    serializer.WriteInt32((int)info.Instance);
                    break;

                case PrimitiveType.UInt64:

                    serializer.WriteInt64((long)(ulong)info.Instance);
                    break;

                case PrimitiveType.Int64:

                    serializer.WriteInt64((long)info.Instance);
                    break;

                case PrimitiveType.Single:

                    serializer.WriteSingle((float)info.Instance);
                    break;

                case PrimitiveType.Double:

                    serializer.WriteDouble((double)info.Instance);
                    break;

                case PrimitiveType.Decimal:

                    serializer.WriteDecimal((decimal)info.Instance);
                    break;
                default:
                    throw new Exception("ABSAVE: Invalid numerical type.");
            }
        }

        public override object Deserialize(in DeserializeInfo info, ref BitSource header)
        {
            var reader = header.Deserializer;

            unchecked
            {
                return _typeCode switch
                {
                    PrimitiveType.Boolean => reader.ReadByte() > 0,
                    //PrimitiveType.IntPtr => IntPtr.Size == 8 ? (IntPtr)reader.ReadInt64() : (IntPtr)reader.ReadInt32(),
                    //PrimitiveType.UIntPtr => UIntPtr.Size == 8 ? (UIntPtr)reader.ReadInt64() : (UIntPtr)reader.ReadInt32(),
                    PrimitiveType.Byte => reader.ReadByte(),
                    PrimitiveType.SByte => (sbyte)reader.ReadByte(),
                    PrimitiveType.UInt16 => reader.ReadInt16(),
                    PrimitiveType.Int16 => reader.ReadInt16(),
                    PrimitiveType.Char => (char)reader.ReadInt16(),
                    PrimitiveType.UInt32 => reader.ReadInt32(),
                    PrimitiveType.Int32 => reader.ReadInt32(),
                    PrimitiveType.UInt64 => reader.ReadInt64(),
                    PrimitiveType.Int64 => reader.ReadInt64(),
                    PrimitiveType.Single => reader.ReadSingle(),
                    PrimitiveType.Double => reader.ReadDouble(),
                    PrimitiveType.Decimal => reader.ReadDecimal(),
                    _ => throw new Exception("Invalid numerical type."),
                };
            }
        }

        enum PrimitiveType
        {
            IntPtr = 1,
            UIntPtr = 2,
            Boolean = 3,
            Char = 4,
            SByte = 5,
            Byte = 6,
            Int16 = 7,
            UInt16 = 8,
            Int32 = 9,
            UInt32 = 10,
            Int64 = 11,
            UInt64 = 12,
            Single = 13,
            Double = 14,
            Decimal = 15,
        }

        public override (ConverterVersionInfo?, bool) GetVersionInfo(uint version) => (null, true);
    }
}
