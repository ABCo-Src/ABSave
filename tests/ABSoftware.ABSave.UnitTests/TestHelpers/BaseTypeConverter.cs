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
    // A type converter with customizable properties for easy testing.
    class BaseTypeConverter : Converter
    {
        public const int OUTPUT_BYTE = 55;

        public static bool WritesToHeader;
        
        public override bool UsesHeaderForVersion(uint version) => WritesToHeader;

        public override bool AlsoConvertsNonExact => true;
        public override Type[] ExactTypes => new Type[] { typeof(BaseIndex), typeof(ConverterValueType) };

        public override bool CheckType(CheckTypeInfo info) =>
            info.Type == typeof(BaseIndex) || info.Type.IsSubclassOf(typeof(BaseIndex));

        public override void Serialize(object obj, Type actualType, ref BitTarget header)
        {
            if (WritesToHeader)
            {
                header.WriteBitOn();
                header.Apply();
            }

            header.Serializer.WriteByte(OUTPUT_BYTE);
        }

        public override object Deserialize(Type actualType, ref BitSource header)
        {
            if (WritesToHeader && !header.ReadBit()) throw new Exception("Deserialize read invalid header bit");
            if (header.Deserializer.ReadByte() != OUTPUT_BYTE) throw new Exception("Deserialize read invalid byte");

            return OUTPUT_BYTE;
        }
    }
}