using ABCo.ABSave.Deserialization;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Mapping.Generation;
using ABCo.ABSave.Serialization;
using System;

namespace ABCo.ABSave.Converters
{
    [Select(typeof(DateTime))]
    [Select(typeof(TimeSpan))]
    public class TickBasedConverter : Converter
    {
        TicksType _type;

        public override void Serialize(in SerializeInfo info, ref BitTarget header)
        {
            switch (_type)
            {
                case TicksType.DateTime:
                    SerializeTicks(((DateTime)info.Instance).Ticks, header.Serializer);
                    break;
                case TicksType.TimeSpan:
                    SerializeTicks(((TimeSpan)info.Instance).Ticks, header.Serializer);
                    break;
            }
        }

        public static void SerializeTicks(long ticks, ABSaveSerializer serializer) => serializer.WriteInt64(ticks);

        public override object Deserialize(in DeserializeInfo info, ref BitSource header) => _type switch
        {
            TicksType.DateTime => new DateTime(DeserializeTicks(header.Deserializer)),
            TicksType.TimeSpan => new TimeSpan(DeserializeTicks(header.Deserializer)),
            _ => throw new Exception("Invalid tick-based type"),
        };

        public static long DeserializeTicks(ABSaveDeserializer deserializer) => deserializer.ReadInt64();

        public override void Initialize(InitializeInfo info) =>
            _type = info.Type == typeof(DateTime) ? TicksType.DateTime : TicksType.TimeSpan;

        enum TicksType
        {
            DateTime,
            TimeSpan
        }
    }
}
