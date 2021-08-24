using ABCo.ABSave.Serialization.Reading;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Mapping.Generation.Converters;
using ABCo.ABSave.Serialization.Writing;
using System;

namespace ABCo.ABSave.Serialization.Converters
{
    [Select(typeof(Version))]
    public class VersionConverter : Converter
    {
        public override void Serialize(in SerializeInfo info) => SerializeVersion((Version)info.Instance, info.Serializer);

        public static void SerializeVersion(Version version, ABSaveSerializer serializer)
        {
            bool hasMajor = version.Major != 1;
            bool hasMinor = version.Minor > 0;
            bool hasBuild = version.Build > 0;
            bool hasRevision = version.Revision > 0;

            serializer.WriteBitWith(hasMajor);
            serializer.WriteBitWith(hasMinor);
            serializer.WriteBitWith(hasBuild);
            serializer.WriteBitWith(hasRevision);

            if (hasMajor) serializer.WriteCompressedInt((uint)version.Major);
            if (hasMinor) serializer.WriteCompressedInt((uint)version.Minor);
            if (hasBuild) serializer.WriteCompressedInt((uint)version.Build);
            if (hasRevision) serializer.WriteCompressedInt((uint)version.Revision);
        }

        public override object Deserialize(in DeserializeInfo info) => DeserializeVersion(info.Deserializer);

        public static Version DeserializeVersion(ABSaveDeserializer deserializer)
        {
            bool hasMajor = deserializer.ReadBit();
            bool hasMinor = deserializer.ReadBit();
            bool hasBuild = deserializer.ReadBit();
            bool hasRevision = deserializer.ReadBit();

            int major = hasMajor ? (int)deserializer.ReadCompressedInt() : 1;
            int minor = hasMinor ? (int)deserializer.ReadCompressedInt() : 0;
            int build = hasBuild ? (int)deserializer.ReadCompressedInt() : 0;
            int revision = hasRevision ? (int)deserializer.ReadCompressedInt() : 0;

            return new Version(major, minor, build, revision);
        }
    }
}
