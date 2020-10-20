using System;

namespace ABSoftware.ABSave.Converters
{
    public class VersionTypeConverter : ABSaveTypeConverter
    {
        public static VersionTypeConverter Instance = new VersionTypeConverter();
        private VersionTypeConverter() { }

        public override bool HasNonExactTypes => false;
        public override Type[] ExactTypes { get; } = new Type[] { typeof(Version) };

        public override void Serialize(object obj, Type type, ABSaveWriter writer)
        {
            var version = (Version)obj;

            var hasMajor = version.Major != 1;
            var hasMinor = version.Minor > 0;
            var hasBuild = version.Build > 0;
            var hasRevision = version.Revision > 0;

            writer.WriteByte((byte)((hasMajor ? 8 : 0) | (hasMinor ? 4 : 0) | (hasBuild ? 2 : 0) | (hasRevision ? 1 : 0)));

            if (hasMajor) writer.WriteInt32((uint)version.Major);
            if (hasMinor) writer.WriteInt32((uint)version.Minor);
            if (hasBuild) writer.WriteInt32((uint)version.Build);
            if (hasRevision) writer.WriteInt32((uint)version.Revision);
        }

        public override object Deserialize(Type type, ABSaveReader reader)
        {
            var firstByte = reader.ReadByte();
            var major = (firstByte & 8) > 0 ? (int)reader.ReadInt32() : 1;
            var minor = (firstByte & 4) > 0 ? (int)reader.ReadInt32() : 0;
            var build = (firstByte & 2) > 0 ? (int)reader.ReadInt32() : 0;
            var revision = (firstByte & 1) > 0 ? (int)reader.ReadInt32() : 0;

            return new Version(major, minor, build, revision);
        }
    }
}
