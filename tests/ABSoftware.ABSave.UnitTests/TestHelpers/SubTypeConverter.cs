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
        private bool _withHeader = false;

        public const int OUTPUT_BYTE = 110;
        public override bool AlsoConvertsNonExact => false;
        public override bool UsesHeaderForVersion(uint version) => true;
        public override Type[] ExactTypes => _withHeader ? new Type[] { typeof(SubWithHeader) } : new Type[] { typeof(SubWithoutHeader) };

        public SubTypeConverter(bool withHeader)
        {
            _withHeader = withHeader;
        }

        public override void Serialize(object obj, Type actualType, ConverterContext context, ref BitTarget header)
        {
            if (((Context)context).WritesToHeader)
            {
                header.WriteBitOn();
                header.Apply();
            }

            header.Serializer.WriteByte(110);
        }

        public override object Deserialize(Type actualType, ConverterContext context, ref BitSource header)
        {
            if (((Context)context).WritesToHeader)
            {
                if (!header.ReadBit()) throw new Exception("Sub deserialization failed.");
                if (header.Deserializer.ReadByte() != OUTPUT_BYTE) throw new Exception("Sub deserialization failed.");

                return new SubWithHeader();
            }

            if (header.Deserializer.ReadByte() != OUTPUT_BYTE) throw new Exception("Sub deserialization failed.");
            return new SubWithoutHeader();
        }

        public override void TryGenerateContext(ref ContextGen gen)
        {
            if (gen.Type == typeof(SubWithHeader))            
                gen.AssignContext(new Context() { WritesToHeader = true }, 0);
            else if (gen.Type == typeof(SubWithoutHeader))
                gen.AssignContext(new Context() { WritesToHeader = false }, 0);
        }

        class Context : ConverterContext
        {
            public bool WritesToHeader;
        }
    }
}
