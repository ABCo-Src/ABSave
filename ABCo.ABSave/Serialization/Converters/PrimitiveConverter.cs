using ABCo.ABSave.Serialization.Reading;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Mapping.Generation.Converters;
using ABCo.ABSave.Serialization.Writing;
using System;

namespace ABCo.ABSave.Serialization.Converters
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

        public override uint Initialize(InitializeInfo info)
        {
            TypeCode typeCode = Type.GetTypeCode(info.Type);

            // IntPtr
            if (typeCode == TypeCode.Object)
                throw new Exception("Unsupported primitive provided. Please note that ABSave does not currently support .NET 5 and above types.");

            _typeCode = (PrimitiveType)typeCode;
            return 0;
        }

        public override bool CheckType(CheckTypeInfo info) => info.Type.IsPrimitive;

        public override void Serialize(in SerializeInfo info)
        {
            if (_typeCode == PrimitiveType.Boolean)
            {
                info.Serializer.WriteBitWith((bool)info.Instance);
                return;
            }

            if (info.Serializer.State.Settings.CompressPrimitives)
                SerializeCompressed(info.Serializer, info.Instance);
            else
                SerializeDirect(info.Serializer, info.Instance);
        }

        void SerializeCompressed(ABSaveSerializer serializer, object instance)
        {
            switch (_typeCode)
            {
                case PrimitiveType.Byte:

                    serializer.WriteByte((byte)instance);
                    break;

                case PrimitiveType.SByte:

                    serializer.WriteByte((byte)instance);
                    break;

                case PrimitiveType.Int16:

                    serializer.WriteCompressedInt((uint)(short)instance);
                    break;

                case PrimitiveType.UInt16:

                    serializer.WriteCompressedInt((ushort)instance);
                    break;

                case PrimitiveType.Char:

                    serializer.WriteCompressedInt((char)instance);
                    break;

                case PrimitiveType.Int32:

                    serializer.WriteCompressedInt((uint)(int)instance);
                    break;

                case PrimitiveType.UInt32:

                    serializer.WriteCompressedInt((uint)instance);
                    break;

                case PrimitiveType.Int64:

                    serializer.WriteCompressedLong((ulong)(long)instance);
                    break;

                case PrimitiveType.UInt64:

                    serializer.WriteCompressedLong((ulong)instance);
                    break;

                case PrimitiveType.Single:

                    serializer.WriteSingle((float)instance);
                    break;

                case PrimitiveType.Double:

                    serializer.WriteDouble((double)instance);
                    break;

                case PrimitiveType.Decimal:

                    serializer.WriteDecimal((decimal)instance);
                    break;
            }
        }

        void SerializeDirect(ABSaveSerializer serializer, object instance)
        {
            switch (_typeCode)
            {
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

                    serializer.WriteByte((byte)instance);
                    break;

                case PrimitiveType.SByte:

                    serializer.WriteByte((byte)(sbyte)instance);
                    break;

                case PrimitiveType.UInt16:

                    serializer.WriteInt16((short)(ushort)instance);
                    break;

                case PrimitiveType.Int16:

                    serializer.WriteInt16((short)instance);
                    break;

                case PrimitiveType.Char:

                    serializer.WriteInt16((short)(char)instance);
                    break;

                case PrimitiveType.UInt32:

                    serializer.WriteInt32((int)(uint)instance);
                    break;

                case PrimitiveType.Int32:

                    serializer.WriteInt32((int)instance);
                    break;

                case PrimitiveType.UInt64:

                    serializer.WriteInt64((long)(ulong)instance);
                    break;

                case PrimitiveType.Int64:

                    serializer.WriteInt64((long)instance);
                    break;

                case PrimitiveType.Single:

                    serializer.WriteSingle((float)instance);
                    break;

                case PrimitiveType.Double:

                    serializer.WriteDouble((double)instance);
                    break;

                case PrimitiveType.Decimal:

                    serializer.WriteDecimal((decimal)instance);
                    break;
                default:
                    throw new Exception("ABSAVE: Invalid numerical type.");
            }
        }

        public override object Deserialize(in DeserializeInfo info)
        {
            if (_typeCode == PrimitiveType.Boolean) return info.Deserializer.ReadBit();

            if (info.Deserializer.State.Settings.CompressPrimitives)
                return DeserializeCompressed(info.Deserializer);
            else
                return DeserializeDirect(info.Deserializer);

        }

        object DeserializeCompressed(ABSaveDeserializer deserializer)
        {
            unchecked
            {
                return _typeCode switch
                {
                    PrimitiveType.Byte => deserializer.ReadByte(),
                    PrimitiveType.SByte => (sbyte)deserializer.ReadByte(),
                    PrimitiveType.UInt16 => (ushort)deserializer.ReadCompressedInt(),
                    PrimitiveType.Int16 => (short)deserializer.ReadCompressedInt(),
                    PrimitiveType.Char => (char)deserializer.ReadCompressedInt(),
                    PrimitiveType.UInt32 => deserializer.ReadCompressedInt(),
                    PrimitiveType.Int32 => (int)deserializer.ReadCompressedInt(),
                    PrimitiveType.UInt64 => deserializer.ReadCompressedLong(),
                    PrimitiveType.Int64 => (long)deserializer.ReadCompressedLong(),
                    PrimitiveType.Single => deserializer.ReadSingle(),
                    PrimitiveType.Double => deserializer.ReadDouble(),
                    PrimitiveType.Decimal => deserializer.ReadDecimal(),
                    _ => throw new Exception("Invalid numerical type."),
                };
            }
        }

        object DeserializeDirect(ABSaveDeserializer deserializer)
        {
            unchecked
            {
                return _typeCode switch
                {
                    //PrimitiveType.IntPtr => IntPtr.Size == 8 ? (IntPtr)reader.ReadInt64() : (IntPtr)reader.ReadInt32(),
                    //PrimitiveType.UIntPtr => UIntPtr.Size == 8 ? (UIntPtr)reader.ReadInt64() : (UIntPtr)reader.ReadInt32(),
                    PrimitiveType.Byte => deserializer.ReadByte(),
                    PrimitiveType.SByte => (sbyte)deserializer.ReadByte(),
                    PrimitiveType.UInt16 => (ushort)deserializer.ReadInt16(),
                    PrimitiveType.Int16 => deserializer.ReadInt16(),
                    PrimitiveType.Char => (char)deserializer.ReadInt16(),
                    PrimitiveType.UInt32 => (uint)deserializer.ReadInt32(),
                    PrimitiveType.Int32 => deserializer.ReadInt32(),
                    PrimitiveType.UInt64 => (ulong)deserializer.ReadInt64(),
                    PrimitiveType.Int64 => deserializer.ReadInt64(),
                    PrimitiveType.Single => deserializer.ReadSingle(),
                    PrimitiveType.Double => deserializer.ReadDouble(),
                    PrimitiveType.Decimal => deserializer.ReadDecimal(),
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

        public override (VersionInfo?, bool) GetVersionInfo(InitializeInfo info, uint version) => (null, true);
    }
}
