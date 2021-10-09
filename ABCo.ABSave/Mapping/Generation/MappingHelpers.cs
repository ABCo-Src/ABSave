using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Mapping.Description.Attributes;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ABCo.ABSave.Mapping.Generation
{
    internal static class MappingHelpers
    {
        public static bool UpdateHighestVersionFromRange(ref uint highestVersion, uint startVer, uint endVer)
        {
            if (endVer <= startVer) throw new InvalidAttributeToVerException();

            // If there is no upper we'll only update the highest version based on what the minimum is, if that's what we're meant to do.
            if (endVer == uint.MaxValue)
            {
                if (startVer > highestVersion)
                    highestVersion = startVer;

                return true;
            }

            // If not, update based on what their custom high is.
            else
            {
                if (endVer > highestVersion)
                    highestVersion = endVer;

                return false;
            }
        }

        public static bool SortVersionedAttributes<T>(out uint highestVersion, T[] attr) where T : AttributeWithVersion
        {
            highestVersion = 0;

            // Sort them by their "FromVer".
            Array.Sort(attr, Comparer<AttributeWithVersion>.Default);

            bool anyUnsetItems = false;
            for (int i = 0; i < attr.Length; i++)
            {
                AttributeWithVersion info = attr[i];
                anyUnsetItems = UpdateHighestVersionFromRange(ref highestVersion, info.FromVer, info.ToVer);
            }

            return anyUnsetItems;
        }

        public static T? FindAttributeForVersion<T>(T[] sortedAttributes, uint version)
            where T : AttributeWithVersion
        {
            if (sortedAttributes == null) return null;

            for (int i = 0; i < sortedAttributes.Length; i++)
            {
                T currentAttribute = sortedAttributes[i];

                if (currentAttribute.FromVer <= version)
                {
                    if (currentAttribute.ToVer > version)
                        return currentAttribute;

                    return null;
                }
            }

            return null;
        }

        public static TValue? GetExistingOrAddNull<TKey, TValue>(Dictionary<TKey, TValue?> dict, TKey key)
            where TKey : notnull
            where TValue : class
        {
            while (true)
            {
                lock (dict)
                {
                    // Does not exist - Has not and is not generating.
                    // Exists but is null - Is currently allocating (if it's a converter)
                    // or is currently generating (if it's a version info), just wait for it to not be null.
                    // Exists and is not null - Is ready to use.
                    if (dict.TryGetValue(key, out TValue? info))
                    {
                        if (info != null) return info;
                    }
                    else
                    {
                        dict.Add(key, null);
                        return null;
                    }
                }

                // Wait a little bit before retrying.
                Thread.Yield();
            }
        }
    }
}
