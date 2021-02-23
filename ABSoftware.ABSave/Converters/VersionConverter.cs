using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Mapping.Generation;
using ABSoftware.ABSave.Serialization;
using System;

namespace ABSoftware.ABSave.Converters
{
    public class VersionConverter : Converter
    {
        public static VersionConverter Instance = new VersionConverter();
        private VersionConverter() { }

        public override bool ConvertsSubTypes => true;
        public override bool AlsoConvertsNonExact => false;
        public override bool WritesToHeader => true;
        public override Type[] ExactTypes { get; } = new Type[] { typeof(Version) };

        public override void Serialize(object obj, Type actualType, IConverterContext context, ref BitTarget header) => SerializeVersion((Version)obj, ref header);

        public void SerializeVersion(Version version, ref BitTarget header)
        {
            var hasMajor = version.Major != 1; 
            var hasMinor = version.Minor > 0;
            var hasBuild = version.Build > 0;
            var hasRevision = version.Revision > 0;

            header.WriteBitWith(hasMajor);
            header.WriteBitWith(hasMinor);
            header.WriteBitWith(hasBuild);
            header.WriteBitWith(hasRevision);

            if (hasMajor) header.Serializer.WriteCompressed((uint)version.Major, ref header);
            if (hasMinor) header.Serializer.WriteCompressed((uint)version.Minor, ref header);
            if (hasBuild) header.Serializer.WriteCompressed((uint)version.Build, ref header);
            if (hasRevision) header.Serializer.WriteCompressed((uint)version.Revision, ref header);

            // If the header hasn't been applied yet, apply it now
            if (header.FreeBits < 8) header.Apply();
        }

        public override object Deserialize(Type actualType, IConverterContext context, ref BitSource header) => DeserializeVersion(ref header);

        public Version DeserializeVersion(ref BitSource header)
        {
            var hasMajor = header.ReadBit();
            var hasMinor = header.ReadBit();
            var hasBuild = header.ReadBit();
            var hasRevision = header.ReadBit();

            var major = hasMajor ? (int)header.Deserializer.ReadCompressedInt(ref header) : 1;
            var minor = hasMinor ? (int)header.Deserializer.ReadCompressedInt(ref header) : 0;
            var build = hasBuild ? (int)header.Deserializer.ReadCompressedInt(ref header) : 0;
            var revision = hasRevision ? (int)header.Deserializer.ReadCompressedInt(ref header) : 0;

            return new Version(major, minor, build, revision);
        }

        public override IConverterContext TryGenerateContext(ref ContextGen gen)
        {
            if (gen.Type == typeof(Version)) gen.MarkCanConvert();

            return null;
        }
    }
}
