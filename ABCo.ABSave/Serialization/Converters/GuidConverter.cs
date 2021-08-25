using ABCo.ABSave.Serialization.Reading;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Serialization.Writing;
using System;

namespace ABCo.ABSave.Serialization.Converters
{
    [Select(typeof(Guid))]
    public class GuidConverter : Converter
    {
        public override void Serialize(in SerializeInfo info)
        {
            var guid = (Guid)info.Instance;

#if NETSTANDARD2_0
            byte[] bytes = guid.ToByteArray();
#else
            Span<byte> bytes = stackalloc byte[16];
            guid.TryWriteBytes(bytes);
#endif

            info.Serializer.WriteRawBytes(bytes);
        }

        public override object Deserialize(in DeserializeInfo info)
        {
#if NETSTANDARD2_0
            byte[] data = new byte[16];
#else
            Span<byte> data = stackalloc byte[16];
#endif

            info.Deserializer.ReadBytes(data);
            return new Guid(data);
        }
    }
}
