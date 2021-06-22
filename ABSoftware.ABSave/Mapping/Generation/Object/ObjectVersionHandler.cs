using ABCo.ABSave.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ABCo.ABSave.Mapping.Generation.Object
{
    /// <summary>
    /// In charge of getting or generating a certain version 
    /// </summary>
    internal static class ObjectVersionHandler
    {
        public static ObjectVersionInfo GetVersionOrAddNull(uint version, ObjectMapItem parentObj)
        {
            if (parentObj.HasOneVersion)
                return version > 0 ? ObjectVersionInfo.None : parentObj.Members.OneVersion;

            while (true)
            {
                lock (parentObj.Members.MultipleVersions)
                {
                    // Does not exist - Has not and is not generating.
                    // Exists but is null - Is currently generating.
                    // Exists and is not null - Is ready to use.
                    if (parentObj.Members.MultipleVersions.TryGetValue(version, out ObjectVersionInfo info))
                    {
                        if (info.Members != null) return info;
                    }
                    else
                    {
                        parentObj.Members.MultipleVersions.Add(version, ObjectVersionInfo.None);
                        return ObjectVersionInfo.None;
                    }
                }

                Thread.Yield();
            }
        }

        public static ObjectVersionInfo AddNewVersion(MapGenerator gen, uint version, ObjectMapItem item)
        {
            if (item.HasOneVersion || version > item.HighestVersion)
                throw new UnsupportedVersionException(item.ItemType, version);

            var newVer = ObjectVersionMapper.GenerateNewVersion(gen, version, item);

            lock (item.Members.MultipleVersions)
            {
                item.Members.MultipleVersions[version] = newVer;
                if (item.Members.MultipleVersions.Count > item.HighestVersion)
                    item.Intermediate.RawMembers = null!;
            }

            return newVer;
        }
    }
}
