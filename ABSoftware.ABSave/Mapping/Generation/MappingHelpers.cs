using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Mapping.Description.Attributes;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ABCo.ABSave.Mapping.Generation
{
    internal static class MappingHelpers
    {
        public static void UpdateHighestVersionFromRange(ref uint highestVersion, uint startVer, uint endVer)
        {
            if (endVer <= startVer) throw new InvalidAttributeToVerException();

            // If there is no upper we'll only update the highest version based on what the minimum is.
            if (endVer == uint.MaxValue)
            {
                if (startVer > highestVersion)
                    highestVersion = startVer;
            }

            // If not, update based on what their custom high is.
            else
            {
                if (endVer > highestVersion)
                    highestVersion = endVer;
            }
        }

        public static void ProcessVersionedAttributes<T>(ref uint highestVersion, T[] attr) where T : AttributeWithVersion
        {
            // Sort them by their "FromVer".
            Array.Sort(attr, Comparer<AttributeWithVersion>.Default);

            for (int i = 0; i < attr.Length; i++)
            {
                AttributeWithVersion info = attr[i];
                UpdateHighestVersionFromRange(ref highestVersion, info.FromVer, info.ToVer);
            }
        }

        public static T? FindAttributeForVersion<T>(T[] attributes, uint version)
            where T : AttributeWithVersion
        {
            if (attributes == null) return null;

            for (int i = 0; i < attributes.Length; i++)
            {
                T currentAttribute = attributes[i];
                if (currentAttribute.FromVer <= version && currentAttribute.ToVer > version)
                    return currentAttribute;
            }

            return null;
        }

        public static TValue? GetExistingOrAddNull<TKey, TValue>(Dictionary<TKey, TValue?> dict, TKey key)
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
