using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Mapping.Representation;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave.Mapping.Caching
{
    // A fast, generic-based cache of type maps.
    internal static class FastMapCache<T>
    {
        static readonly object _lock = new object();
        static ABSaveMapItem _defaultCache;
        static Dictionary<BindingFlags, ABSaveMapItem> _uniqueCaches;

        internal static ABSaveMapItem LoadCache(ABSaveSettings settings)
        {
            if (settings.MemberReflectionFlags == ABSaveUtils.DefaultBindingFlags) return _defaultCache;

            if (_uniqueCaches == null)
            {
                lock (_lock)
                {
                    _uniqueCaches = new Dictionary<BindingFlags, ABSaveMapItem>();
                }
                return null;
            }

            lock (_lock)
            {
                if (_uniqueCaches.TryGetValue(settings.MemberReflectionFlags, out ABSaveMapItem info))
                    return info;
            }

            // We failed to find one quickly, attempt to get it from the slow cache instead.
            return SlowMapCache.LoadCache(typeof(T), settings);
        }

        internal static void SaveCache(ABSaveMapItem info, ABSaveSettings settings)
        {
            lock (_lock)
            {
                if (settings.MemberReflectionFlags == ABSaveUtils.DefaultBindingFlags) _defaultCache = info;
                else
                {
                    _uniqueCaches ??= new Dictionary<BindingFlags, ABSaveMapItem>();
                    _uniqueCaches[settings.MemberReflectionFlags] = info;
                }
            }

            // Also add it to the slow cache.
            SlowMapCache.SaveCache(typeof(T), info, settings);
        }
    }
}
