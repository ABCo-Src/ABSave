using ABCo.ABSave.Converters;
using ABCo.ABSave.Deserialization;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
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
    [Select(typeof(BaseIndex))]
    [Select(typeof(ConverterValueType))]
    [SelectOtherWithCheckType]
    class BaseTypeConverter : Converter
    {
        public const int OUTPUT_BYTE = 55;

        public static bool WritesToHeader;

        public override (VersionInfo, bool) GetVersionInfo(InitializeInfo info, uint version) => (null, WritesToHeader);

        public override bool CheckType(CheckTypeInfo info) =>
            info.Type == typeof(BaseIndex) || info.Type.IsSubclassOf(typeof(BaseIndex));

        public override void Serialize(in SerializeInfo info, ref BitTarget header)
        {
            if (WritesToHeader)
            {
                header.WriteBitOn();
                header.Apply();
            }

            header.Serializer.WriteByte(OUTPUT_BYTE);
        }

        public override object Deserialize(in DeserializeInfo info, ref BitSource header)
        {
            if (WritesToHeader && !header.ReadBit()) throw new Exception("Deserialize read invalid header bit");
            if (header.Deserializer.ReadByte() != OUTPUT_BYTE) throw new Exception("Deserialize read invalid byte");

            return OUTPUT_BYTE;
        }
    }
}