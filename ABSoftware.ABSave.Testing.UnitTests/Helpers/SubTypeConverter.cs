using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABSoftware.ABSave.Testing.UnitTests.Helpers
{
    public class SubTypeConverter : ABSaveConverter
    {
        private bool _withHeader = false;

        public const int OUTPUT_BYTE = 110;
        public override bool ConvertsSubTypes => false;
        public override bool AlsoConvertsNonExact => false;
        public override bool WritesToHeader => _withHeader;
        public override Type[] ExactTypes => _withHeader ? new Type[] { typeof(SubWithHeader) } : new Type[] { typeof(SubWithoutHeader) };

        public SubTypeConverter(bool withHeader)
        {
            _withHeader = withHeader;
        }

        public override void Serialize(object obj, Type actualType, IABSaveConverterContext context, ref BitTarget header)
        {
            if (((Context)context).WritesToHeader)
            {
                header.WriteBitOn();
                header.Apply();
            }

            header.Serializer.WriteByte(110);
        }

        public override object Deserialize(Type actualType, IABSaveConverterContext context, ref BitSource header)
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

        public override IABSaveConverterContext TryGenerateContext(ABSaveMap map, Type type)
        {
            if (type == typeof(SubWithHeader))
                return new Context() { WritesToHeader = true };
            else if (type == typeof(SubWithoutHeader))
                return new Context() { WritesToHeader = false };
            else
                return null;
        }

        class Context : IABSaveConverterContext
        {
            public bool WritesToHeader;
        }
    }
}
