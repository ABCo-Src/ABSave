using ABCo.ABSave.Deserialization;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Mapping.Generation;
using ABCo.ABSave.Serialization;
using System;

namespace ABCo.ABSave.Converters
{
    [Select(typeof(Version))]
    public class VersionConverter : Converter
    {
        public override void Serialize(in SerializeInfo info, ref BitTarget header) => SerializeVersion((Version)info.Instance, ref header);

        public static void SerializeVersion(Version version, ref BitTarget header)
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

        public override object Deserialize(in DeserializeInfo info, ref BitSource header) => DeserializeVersion(ref header);

        public static Version DeserializeVersion(ref BitSource header)
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

        public override bool UsesHeaderForVersion(uint version) => true;
    }
}
