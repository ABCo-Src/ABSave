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
                    SerializeTicks(((DateTime)info.Instance).Ticks, info.Serializer);
                    break;
                case TicksType.TimeSpan:
                    SerializeTicks(((TimeSpan)info.Instance).Ticks, info.Serializer);
                    break;
            }
        }

        public static void SerializeTicks(long ticks, ABSaveSerializer serializer)
        {
            if (serializer.State.Settings.CompressPrimitives)
                serializer.WriteCompressedLong((ulong)ticks);
            else
                serializer.WriteInt64(ticks);
        }

        public override object Deserialize(in DeserializeInfo info) => _type switch
        {
            TicksType.DateTime => new DateTime(DeserializeTicks(info.Deserializer)),
            TicksType.TimeSpan => new TimeSpan(DeserializeTicks(info.Deserializer)),
            _ => throw new Exception("Invalid tick-based type"),
        };

        public static long DeserializeTicks(ABSaveDeserializer serializer)
        {
            if (serializer.State.Settings.CompressPrimitives)
                return (long)serializer.ReadCompressedLong();
            else
                return serializer.ReadInt64();
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
