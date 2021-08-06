using ABCo.ABSave.Converters;
using ABCo.ABSave.Deserialization;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Mapping.Generation.Converters;
using ABCo.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABCo.ABSave.UnitTests.TestHelpers
{
    [Select(typeof(KeySubFirst))]
    [Select(typeof(KeySubSecond))]
    [Select(typeof(IndexKeySubIndex))]
    [Select(typeof(IndexKeySubKey))]
    [Select(typeof(ClassWithMinVersion))]
    public class OtherTypeConverter : Converter
    {
        public static bool WritesToHeader;
        public const int OUTPUT_BYTE = 155;

        public override (VersionInfo, bool) GetVersionInfo(InitializeInfo info, uint version) => (null, WritesToHeader);

        public override void Serialize(in SerializeInfo info, ref BitTarget header)
        {
            if (WritesToHeader)
            {
                header.WriteBitOn();
                header.Apply();
            }

            header.Serializer.WriteByte(OUTPUT_BYTE);
        }

        public override object Deserialize(in DeserializeInfo info)
        {
            if (WritesToHeader && !info.Header.ReadBit()) throw new Exception("Deserialize read invalid header bit");

            var deserializer = info.Header.Finish();
            if (deserializer.ReadByte() != OUTPUT_BYTE) throw new Exception("Deserialize read invalid byte");

            return Activator.CreateInstance(info.ActualType);
        }
    }
}
