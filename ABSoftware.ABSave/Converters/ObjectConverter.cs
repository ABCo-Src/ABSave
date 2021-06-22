using ABCo.ABSave.Deserialization;
using ABCo.ABSave.Mapping.Generation;
using ABCo.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Converters
{
    public class ObjectConverter : Converter
    {
        internal ObjectIntermediateItem[]? _rawMembers;

        public override void Serialize(in SerializeInfo info, ref BitTarget header)
        {
            throw new NotImplementedException();
        }

        public override object Deserialize(in DeserializeInfo info, ref BitSource header)
        {
            throw new NotImplementedException();
        }
    }
}
