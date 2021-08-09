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
    [Select(typeof(SubWithHeader2))]
    [Select(typeof(SubWithoutHeader))]
    [Select(typeof(SubWithoutHeader2))]
    public class SubTypeConverter : Converter
    {
        bool _writesToHeader;
        bool _isNo2;

        public const int OUTPUT_BYTE = 110;
        public override (VersionInfo, bool) GetVersionInfo(InitializeInfo info, uint version) => (null, _writesToHeader);

        public override void Serialize(in SerializeInfo info)
        {
            if (_writesToHeader)
            {
                info.Header.WriteBitOn();
                info.Header.MoveToNextByte();
            }

            var serializer = info.Header.Finish();
            serializer.WriteByte(OUTPUT_BYTE);
        }

        public override object Deserialize(in DeserializeInfo info)
        {
            if (_writesToHeader)
            {
                if (!info.Header.ReadBit()) throw new Exception("Sub deserialization failed.");

                var deserializer = info.Header.Finish();
                if (deserializer.ReadByte() != OUTPUT_BYTE) throw new Exception("Sub deserialization failed.");

                return _isNo2 ? new SubWithHeader2() : new SubWithHeader();
            }

            {
                var deserializer = info.Header.Finish();
                if (deserializer.ReadByte() != OUTPUT_BYTE) throw new Exception("Sub deserialization failed.");
            }

            return _isNo2 ? new SubWithoutHeader2() : new SubWithoutHeader();
        }

        public override uint Initialize(InitializeInfo info)
        {
            _isNo2 = info.Type == typeof(SubWithHeader2) || info.Type == typeof(SubWithoutHeader2);
            _writesToHeader = info.Type == typeof(SubWithHeader) || info.Type == typeof(SubWithHeader2);
            return 0;
        }
    }
}
