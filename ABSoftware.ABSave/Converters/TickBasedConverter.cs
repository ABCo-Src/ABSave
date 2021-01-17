using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Converters
{
    public class TickBasedConverter : ABSaveConverter
    {
        public static TickBasedConverter Instance { get; } = new TickBasedConverter();
        private TickBasedConverter() { }

        public override bool ConvertsSubTypes => true;
        public override bool AlsoConvertsNonExact => false;
        public override Type[] ExactTypes { get; } = new Type[]
        {
            typeof(DateTime),
            typeof(TimeSpan)
        };

        public override void Serialize(object obj, Type actualType, IABSaveConverterContext context, ref BitTarget header)
        {
            var asContext = (Context)context;

            switch (asContext.Type)
            {
                case TicksType.DateTime:
                    SerializeTicks(((DateTime)obj).Ticks, header.Serializer);
                    break;
                case TicksType.TimeSpan:
                    SerializeTicks(((TimeSpan)obj).Ticks, header.Serializer);
                    break;
            }
        }

        public void SerializeTicks(long ticks, ABSaveSerializer serializer) => serializer.WriteInt64(ticks);

        public override object Deserialize(Type actualType, IABSaveConverterContext context, ref BitSource header)
        {
            var asContext = (Context)context;

            return asContext.Type switch
            {
                TicksType.DateTime => new DateTime(DeserializeTicks(header.Deserializer)),
                TicksType.TimeSpan => new TimeSpan(DeserializeTicks(header.Deserializer)),
                _ => throw new Exception("Invalid tick-based type"),
            };
        }

        public long DeserializeTicks(ABSaveDeserializer deserializer) => deserializer.ReadInt64();

        public override IABSaveConverterContext TryGenerateContext(ABSaveMap map, Type type)
        {
            if (type == typeof(DateTime))
                return Context.DateTime;
            else if (type == typeof(TimeSpan))
                return Context.TimeSpan;
            else
                return null;
        }

        enum TicksType
        {
            DateTime,
            TimeSpan
        }

        class Context : IABSaveConverterContext
        {
            public static Context DateTime = new Context() { Type = TicksType.DateTime };
            public static Context TimeSpan = new Context() { Type = TicksType.TimeSpan };

            public TicksType Type;
        }
    }
}
