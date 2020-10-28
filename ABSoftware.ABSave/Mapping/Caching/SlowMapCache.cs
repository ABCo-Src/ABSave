using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Mapping.Representation;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave.Mapping.Caching
{
    // A slow, but non-generic dependant, cache of maps for types.
    internal static class SlowMapCache
    {
        static readonly object _lock = new object();
        static readonly Dictionary<Type, ABSaveMapItem> DefaultCache = new Dictionary<Type, ABSaveMapItem>();
        static readonly Dictionary<BindingFlags, Dictionary<Type, ABSaveMapItem>> UniqueCaches = new Dictionary<BindingFlags, Dictionary<Type, ABSaveMapItem>>();

        internal static ABSaveMapItem LoadCache(Type type, ABSaveSettings settings)
        {
            lock (_lock)
            {
                Dictionary<Type, ABSaveMapItem> cacheContainer;

                if (settings.MemberReflectionFlags == ABSaveUtils.DefaultBindingFlags) cacheContainer = DefaultCache;
                else
                {
                    if (!UniqueCaches.TryGetValue(settings.MemberReflectionFlags, out cacheContainer))
                        return null;
                }

                if (cacheContainer.TryGetValue(type, out ABSaveMapItem info))
                    return info;
                else
                    return null;
            }
            
        }

        internal static void SaveCache(Type type, ABSaveMapItem info, ABSaveSettings settings)
        {
            lock (_lock)
            {
                if (settings.MemberReflectionFlags == ABSaveUtils.DefaultBindingFlags) DefaultCache[type] = info;
                else
                {
                    if (!UniqueCaches.TryGetValue(settings.MemberReflectionFlags, out var cacheContainer))
                    {
                        cacheContainer = new Dictionary<Type, ABSaveMapItem>();
                        UniqueCaches.Add(settings.MemberReflectionFlags, cacheContainer);
                    }

                    cacheContainer.Add(type, info);
                }
            }
        }
    }
}
