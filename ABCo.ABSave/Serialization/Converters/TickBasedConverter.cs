using ABCo.ABSave.Serialization.Reading;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Mapping.Generation.Converters;
using ABCo.ABSave.Serialization.Writing;
using System;

namespace ABCo.ABSave.Serialization.Converters
{
    [Select(typeof(DateTime))]
    [Select(typeof(TimeSpan))]
    public class TickBasedConverter : Converter
    {
        TicksType _type;

        public override void Serialize(in SerializeInfo info)
        {
            switch (_type)
            {
                case TicksType.DateTime:
                    SerializeTicks(((DateTime)info.Instance).Ticks, info.Header);
                    break;
                case TicksType.TimeSpan:
                    SerializeTicks(((TimeSpan)info.Instance).Ticks, info.Header);
                    break;
            }
        }

        public static void SerializeTicks(long ticks, ABSaveSerializer writer)
        {
            if (writer.State.Settings.CompressPrimitives)
                writer.WriteCompressedLong((ulong)ticks);
            else
                writer.WriteInt64(ticks);
        }

        public override object Deserialize(in DeserializeInfo info) => _type switch
        {
            TicksType.DateTime => new DateTime(DeserializeTicks(info.Header)),
            TicksType.TimeSpan => new TimeSpan(DeserializeTicks(info.Header)),
            _ => throw new Exception("Invalid tick-based type"),
        };

        public static long DeserializeTicks(ABSaveDeserializer header)
        {
            if (header.State.Settings.CompressPrimitives)
                return (long)header.ReadCompressedLong();
            else
                return header.ReadInt64();
        }

        public override uint Initialize(InitializeInfo info)
        {
            _type = info.Type == typeof(DateTime) ? TicksType.DateTime : TicksType.TimeSpan;
            return 0;
        }

        enum TicksType
        {
            DateTime,
            TimeSpan
        }
    }
}
