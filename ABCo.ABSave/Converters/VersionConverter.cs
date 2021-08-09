using ABCo.ABSave.Deserialization;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Mapping.Generation.Converters;
using ABCo.ABSave.Serialization;
using System;

namespace ABCo.ABSave.Converters
{
    [Select(typeof(Version))]
    public class VersionConverter : Converter
    {
        public override void Serialize(in SerializeInfo info) => SerializeVersion((Version)info.Instance, info.Header);

        public static void SerializeVersion(Version version, BitWriter header)
        {
            bool hasMajor = version.Major != 1;
            bool hasMinor = version.Minor > 0;
            bool hasBuild = version.Build > 0;
            bool hasRevision = version.Revision > 0;

            header.WriteBitWith(hasMajor);
            header.WriteBitWith(hasMinor);
            header.WriteBitWith(hasBuild);
            header.WriteBitWith(hasRevision);

            if (hasMajor) header.WriteCompressedInt((uint)version.Major);
            if (hasMinor) header.WriteCompressedInt((uint)version.Minor);
            if (hasBuild) header.WriteCompressedInt((uint)version.Build);
            if (hasRevision) header.WriteCompressedInt((uint)version.Revision);

            // If the header hasn't been applied yet, apply it now
            if (header.FreeBits < 8) header.MoveToNextByte();
        }

        public override object Deserialize(in DeserializeInfo info) => DeserializeVersion(info.Header);

        public static Version DeserializeVersion(BitReader header)
        {
            bool hasMajor = header.ReadBit();
            bool hasMinor = header.ReadBit();
            bool hasBuild = header.ReadBit();
            bool hasRevision = header.ReadBit();

            int major = hasMajor ? (int)header.ReadCompressedInt() : 1;
            int minor = hasMinor ? (int)header.ReadCompressedInt() : 0;
            int build = hasBuild ? (int)header.ReadCompressedInt() : 0;
            int revision = hasRevision ? (int)header.ReadCompressedInt() : 0;

            return new Version(major, minor, build, revision);
        }

        public override (VersionInfo?, bool) GetVersionInfo(InitializeInfo info, uint version) => (null, true);
    }
}
