using ABCo.ABSave.Deserialization;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Serialization;
using System;

namespace ABCo.ABSave.Converters
{
    [Select(typeof(Guid))]
    public class GuidConverter : Converter
    {
        public override void Serialize(in SerializeInfo info, BitWriter header)
        {
            var serializer = header.Finish();

            var guid = (Guid)info.Instance;

            Span<byte> bytes = stackalloc byte[16];
            guid.TryWriteBytes(bytes);

            serializer.WriteBytes(bytes);
        }

        public override object Deserialize(in DeserializeInfo info)
        {
            Span<byte> data = stackalloc byte[16];

            var deserializer = info.Header.Finish();
            deserializer.ReadBytes(data);
            return new Guid(data);
        }
    }
}
