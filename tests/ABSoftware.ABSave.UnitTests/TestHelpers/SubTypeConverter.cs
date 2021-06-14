using ABCo.ABSave.Converters;
using ABCo.ABSave.Deserialization;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Generation;
using ABCo.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABCo.ABSave.UnitTests.TestHelpers
{
    public class SubTypeConverter : Converter
    {
        bool _writesToHeader;

        public const int OUTPUT_BYTE = 110;
        public override bool AlsoConvertsNonExact => false;
        public override bool UsesHeaderForVersion(uint version) => true;
        public override Type[] ExactTypes => new Type[] { typeof(SubWithHeader), typeof(SubWithoutHeader) };

        public override void Serialize(object obj, Type actualType, ref BitTarget header)
        {
            if (_writesToHeader)
            {
                header.WriteBitOn();
                header.Apply();
            }

            header.Serializer.WriteByte(OUTPUT_BYTE);
        }

        public override object Deserialize(Type actualType, ref BitSource header)
        {
            if (_writesToHeader)
            {
                if (!header.ReadBit()) throw new Exception("Sub deserialization failed.");
                if (header.Deserializer.ReadByte() != OUTPUT_BYTE) throw new Exception("Sub deserialization failed.");

                return new SubWithHeader();
            }

            if (header.Deserializer.ReadByte() != OUTPUT_BYTE) throw new Exception("Sub deserialization failed.");
            return new SubWithoutHeader();
        }
        
        public override void Initialize(InitializeInfo info) =>        
            _writesToHeader = info.Type == typeof(SubWithHeader);
    }
}
