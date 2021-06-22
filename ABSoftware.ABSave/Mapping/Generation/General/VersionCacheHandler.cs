using ABCo.ABSave.Converters;
using ABCo.ABSave.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
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
            dest.HasOneVersion = false;
            dest.VersionCache.MultipleVersions = new Dictionary<uint, VersionInfo?>();

            // Generate the highest version.
            AddNewVersion(dest, dest.HighestVersion, gen);
        }

        static void FillDestWithOneVersion(Converter dest, MapGenerator gen)
        {
            dest.HasOneVersion = true;
            dest.VersionCache.OneVersion = GetVersionInfo(dest, 0);

            // There are no more versions here, so call the release for that.
            dest.HandleAllVersionsGenerated();
        }

        public static VersionInfo? GetVersionOrAddNull(Converter item, uint version)
        {
            if (item.HasOneVersion)
            {
                Debug.Assert(version != 0);
                return version > 0 ? null : item.VersionCache.OneVersion;
            }

            while (true)
            {
                lock (item.VersionCache.MultipleVersions)
                {
                    // Does not exist - Has not and is not generating.
                    // Exists but is null - Is currently generating.
                    // Exists and is not null - Is ready to use.
                    if (item.VersionCache.MultipleVersions.TryGetValue(version, out VersionInfo? info))
                    {
                        if (info != null) return info;
                    }
                    else
                    {
                        item.VersionCache.MultipleVersions.Add(version, null);
                        return null;
                    }
                }

                Thread.Yield();
            }
        }

        public static VersionInfo AddNewVersion(Converter converter, uint version, MapGenerator gen)
        {
            if (converter.HasOneVersion || version > converter.HighestVersion)
                throw new UnsupportedVersionException(converter.ItemType, version);

            var newVer = GetVersionInfo(converter, version);

            lock (converter.VersionCache.MultipleVersions)
            {
                converter.VersionCache.MultipleVersions[version] = newVer;
                if (converter.VersionCache.MultipleVersions.Count > converter.HighestVersion)
                    converter.HandleAllVersionsGenerated();
            }

            return newVer;
        }

        static VersionInfo GetVersionInfo(Converter converter, uint version)
        {
            var (newVer, usesHeader) = converter.GetVersionInfo(version);

            newVer ??= new VersionInfo(usesHeader);
            newVer.Initialize(version, usesHeader, converter);

            return newVer;
        }
    }
}
