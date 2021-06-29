using ABCo.ABSave.Converters;
using ABCo.ABSave.Deserialization;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Mapping.Generation.Converters;
using ABCo.ABSave.Serialization;
using System;

namespace ABCo.ABSave.UnitTests.TestHelpers
{
    [Select(typeof(SubWithHeader))]
    [Select(typeof(SubWithoutHeader))]
    public class SubTypeConverter : Converter
    {
        bool _writesToHeader;

        public const int OUTPUT_BYTE = 110;
        public override (VersionInfo, bool) GetVersionInfo(InitializeInfo info, uint version) => (null, true);

        public override void Serialize(in SerializeInfo info, ref BitTarget header)
        {
            if (_writesToHeader)
            {
                header.WriteBitOn();
                header.Apply();
            }

            header.Serializer.WriteByte(OUTPUT_BYTE);
        }

        public override object Deserialize(in DeserializeInfo info, ref BitSource header)
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

        public override uint Initialize(InitializeInfo info)
        {
            _writesToHeader = info.Type == typeof(SubWithHeader);
            return 0;
        }
    }
}
