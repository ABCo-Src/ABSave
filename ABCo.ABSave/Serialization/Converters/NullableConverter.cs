using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Mapping.Generation.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Serialization.Converters
{
    [Select(typeof(Nullable<>), 0)]
    public class NullableConverter : Converter
    {
        Converter _innerItem;

        public override uint Initialize(InitializeInfo info)
        {
            _innerItem = info.GetMap(info.Type.GetGenericArguments()[0]);
        }

        public override void Serialize(in SerializeInfo info)
        {
            // The fact that we made it to the converter means it wasn't null, so write it now.
            info.Serializer.WriteBitOn();
            info.Serializer.WriteExactNonNullItem(info.Instance, _innerItem);
        }

        public override object? Deserialize(in DeserializeInfo info)
        {
            if (info.Deserializer.ReadBit())
                return info.Deserializer.ReadExactNonNullItem(_innerItem);

            return null;
        }
    }
}
