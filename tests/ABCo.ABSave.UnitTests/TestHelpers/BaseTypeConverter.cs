using ABCo.ABSave.Serialization.Converters;
using ABCo.ABSave.Serialization.Reading;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Mapping.Generation.Converters;
using ABCo.ABSave.Serialization.Writing;
using System;

namespace ABCo.ABSave.UnitTests.TestHelpers
{
    // A type converter with customizable properties for easy testing.
    [Select(typeof(BaseIndex))]
    [Select(typeof(KeyBase))]
    [Select(typeof(ConverterValueType))]
    [SelectOtherWithCheckType]
    class BaseTypeConverter : Converter
    {
        public const int OUTPUT_BYTE = 55;

        public static bool WritesToHeader;

        public override (VersionInfo, bool) GetVersionInfo(InitializeInfo info, uint version) => (null, WritesToHeader);

        public override bool CheckType(CheckTypeInfo info) =>
            info.Type == typeof(BaseIndex) || info.Type.IsSubclassOf(typeof(BaseIndex));

        public override void Serialize(in SerializeInfo info)
        {
            if (WritesToHeader)
            {
                info.Header.WriteBitOn();
                info.Header.MoveToNextByte();
            }

            var serializer = info.Header.Finish();
            serializer.WriteByte(OUTPUT_BYTE);
        }

        public override object Deserialize(in DeserializeInfo info)
        {
            if (WritesToHeader && !info.Header.ReadBit()) throw new Exception("Deserialize read invalid header bit");

            var deserializer = info.Header.Finish();
            if (deserializer.ReadByte() != OUTPUT_BYTE) throw new Exception("Deserialize read invalid byte");

            return OUTPUT_BYTE;
        }
    }
}