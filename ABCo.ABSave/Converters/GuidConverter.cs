using ABCo.ABSave.Deserialization;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Serialization;
using System;

namespace ABCo.ABSave.Converters
{
    [Select(typeof(Guid))]
    public class GuidConverter : Converter
    {
        public override void Serialize(in SerializeInfo info, ref BitTarget header)
        {
            var guid = (Guid)info.Instance;
            Span<byte> bytes = stackalloc byte[16];

            guid.TryWriteBytes(bytes);
            header.Serializer.WriteBytes(bytes);
        }

        public override object Deserialize(in DeserializeInfo info, ref BitSource header)
        {
            Span<byte> data = stackalloc byte[16];
            header.Deserializer.ReadBytes(data);
            return new Guid(data);
        }
    }
}
