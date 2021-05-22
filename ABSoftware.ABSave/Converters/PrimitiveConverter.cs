using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Mapping.Generation;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Converters
{
    // TODO: Add .NET 5 support for "nint"s
    public class PrimitiveConverter : Converter
    {
        public static PrimitiveConverter Instance { get; } = new PrimitiveConverter();
        private PrimitiveConverter() { }

        public override bool WritesToHeader => false;
        public override bool ConvertsSubTypes => false;
        public override bool AlsoConvertsNonExact => true;

        public override Type[] ExactTypes { get; } = new Type[]
        {
            typeof(byte),
            typeof(sbyte),
            typeof(char),
            typeof(ushort),
            typeof(short),
            typeof(uint),
            typeof(int),
            typeof(ulong),
            typeof(long),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(IntPtr),
            typeof(UIntPtr),
            typeof(bool)
        };

        public override void Serialize(object obj, Type actualType, ConverterContext context, ref BitTarget header)
        {
            var serializer = header.Serializer;

            switch (((Context)context).Type)
            {
                case PrimitiveType.Boolean:
                    var bl = (bool)obj;
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

                    serializer.WriteByte((byte)obj);
                    break;

                case PrimitiveType.SByte:

                    serializer.WriteByte((byte)(sbyte)obj);
                    break;

                case PrimitiveType.UInt16:

                    serializer.WriteInt16((short)(ushort)obj);
                    break;

                case PrimitiveType.Int16:

                    serializer.WriteInt16((short)obj);
                    break;

                case PrimitiveType.Char:

                    serializer.WriteInt16((short)(char)obj);
                    break;

                case PrimitiveType.UInt32:

                    serializer.WriteInt32((int)(uint)obj);
                    break;

                case PrimitiveType.Int32:

                    serializer.WriteInt32((int)obj);
                    break;

                case PrimitiveType.UInt64:

                    serializer.WriteInt64((long)(ulong)obj);
                    break;

                case PrimitiveType.Int64:

                    serializer.WriteInt64((long)obj);
                    break;

                case PrimitiveType.Single:

                    serializer.WriteSingle((float)obj);
                    break;

                case PrimitiveType.Double:

                    serializer.WriteDouble((double)obj);
                    break;

                case PrimitiveType.Decimal:

                    serializer.WriteDecimal((decimal)obj);
                    break;
                default:
                    throw new Exception("ABSAVE: Invalid numerical type.");
            }
        }

        public override object Deserialize(Type actualType, ConverterContext context, ref BitSource header)
        {
            var reader = header.Deserializer;

            unchecked
            {
                return ((Context)context).Type switch
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

        public override void TryGenerateContext(ref ContextGen gen)
        {
            if (!gen.Type.IsPrimitive) return;

            var typeCode = Type.GetTypeCode(gen.Type);

            // IntPtr
            if (typeCode == TypeCode.Object)
                throw new Exception("Unsupported primitive provided. Please note that ABSave does not currently support .NET 5 and above types.");

            gen.AssignContext(new Context((PrimitiveType)typeCode));
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

        class Context : ConverterContext
        {
            public PrimitiveType Type;

            public Context(PrimitiveType type) => Type = type;
        }
    }
}
