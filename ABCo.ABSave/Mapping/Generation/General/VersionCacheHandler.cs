using ABCo.ABSave.Serialization.Converters;
using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Mapping.Description.Attributes;
using ABCo.ABSave.Mapping.Generation.Converters;
using ABCo.ABSave.Mapping.Generation.Inheritance;
using System.Collections.Generic;
using System.Threading;

namespace ABCo.ABSave.Mapping.Generation.General
{
    /// <summary>
    /// In charge of managing the version cache within objects.
    /// </summary>
    internal static class VersionCacheHandler
    {
        public static void SetupVersionCacheOnItem(Converter dest, MapGenerator gen)
        {
            if (dest.HighestVersion == 0)
                FillDestWithOneVersion(dest, gen);
            else
                FillDestWithMultipleVersions(dest, gen);
        }

        static void FillDestWithMultipleVersions(Converter dest, MapGenerator gen)
        {
            dest._hasOneVersion = false;
            dest._versionCache.MultipleVersions = new Dictionary<uint, VersionInfo?>();

            // Generate the highest version.
            AddNewVersion(dest, dest.HighestVersion, gen);
        }

        static void FillDestWithOneVersion(Converter dest, MapGenerator gen)
        {
            dest._hasOneVersion = true;
            dest._versionCache.OneVersion = GetVersionInfo(dest, 0, gen);

            // There are no more versions here, so call the release for that.
            dest.HandleAllVersionsGenerated();
        }

        public static VersionInfo? GetVersionOrAddNull(Converter item, uint version)
        {
            if (item._hasOneVersion)
                return version > 0 ? null : item._versionCache.OneVersion;

            return MappingHelpers.GetExistingOrAddNull(item._versionCache.MultipleVersions, version);
        }

        public static VersionInfo AddNewVersion(Converter converter, uint version, MapGenerator gen)
        {
            if (converter._hasOneVersion || version > converter.HighestVersion)
                throw new UnsupportedVersionException(converter.ItemType, version);

            VersionInfo? newVer = GetVersionInfo(converter, version, gen);

            lock (converter._versionCache.MultipleVersions)
            {
                converter._versionCache.MultipleVersions[version] = newVer;
                if (converter._versionCache.MultipleVersions.Count > converter.HighestVersion)
                    converter.HandleAllVersionsGenerated();
            }

            return newVer;
        }

        static VersionInfo GetVersionInfo(Converter converter, uint version, MapGenerator gen)
        {
            (VersionInfo? newVer, bool usesHeader) = converter.GetVersionInfo(new InitializeInfo(converter.ItemType, gen), version);

            SaveInheritanceAttribute? inheritanceInfo = null;
            if (converter._allInheritanceAttributes != null)
                inheritanceInfo = MappingHelpers.FindAttributeForVersion(converter._allInheritanceAttributes, version);

            newVer ??= new VersionInfo(usesHeader);
            newVer.Assign(version, usesHeader, inheritanceInfo);

            return newVer;
        }
    }
}
