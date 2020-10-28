using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Mapping.Caching;
using ABSoftware.ABSave.Mapping.Representation;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace ABSoftware.ABSave.Mapping.Generation
{
    public static class ABSaveMapGenerator
    {
        public static ABSaveMapItem Generate<T>(ABSaveSettings settings)
        {
            // Try get from cache
            var cache = FastMapCache<T>.LoadCache(settings);
            if (cache != null)
            {
                Interlocked.Increment(ref cache.UsageCount);
                return cache;
            }

            // Generate and save to cache
            if (ABSaveUtils.TryFindConverterForType(settings, typeof(T), out ABSaveTypeConverter converter))
            {
                return GenerateConverterMap<T>(settings, converter);
            }
            else
                return GenerateObjectMap<T>(settings);
        }

        public static ABSaveMapItem GenerateNonGeneric(ABSaveSettings settings, Type type)
        {
            // Try get from cache
            var cache = SlowMapCache.LoadCache(type, settings);
            if (cache != null)
            {
                Interlocked.Increment(ref cache.UsageCount);
                return cache;
            }

            // Generate and save to cache
            if (ABSaveUtils.TryFindConverterForType(settings, type, out ABSaveTypeConverter converter))
            {
                return GenerateConverterMap(settings, type, converter);
            }
            else
                return GenerateObjectMap(settings, type);
        }

        static ABSaveMapItem GenerateConverterMap<T>(ABSaveSettings settings, ABSaveTypeConverter converter)
        {
            var res = new ABSaveConverterMapItem(converter);
            FastMapCache<T>.SaveCache(res, settings);
            return res;
        }

        static ABSaveMapItem GenerateConverterMap(ABSaveSettings settings, Type type, ABSaveTypeConverter converter)
        {
            var res = new ABSaveConverterMapItem(converter);
            SlowMapCache.SaveCache(type, res, settings);
            return res;
        }

        // These save to the cache before the item is even made. This will allow us to not only allow us to generate quicker, but will stop infinite recursion too.
        static ABSaveObjectMapItem GenerateObjectMap<T>(ABSaveSettings settings)
        {
            var res = new ABSaveObjectMapItem();

            FastMapCache<T>.SaveCache(res, settings);
            return PopulateObjectMapItem(res, settings, typeof(T));
        }

        static ABSaveObjectMapItem GenerateObjectMap(ABSaveSettings settings, Type type)
        {
            var res = new ABSaveObjectMapItem();

            SlowMapCache.SaveCache(type, res, settings);
            return PopulateObjectMapItem(res, settings, type);
        }

        static ABSaveObjectMapItem PopulateObjectMapItem(ABSaveObjectMapItem item, ABSaveSettings settings, Type type)
        {
            var fields = type.GetFields(settings.MemberReflectionFlags);
            item.Fields = new ObjectFieldInfo[fields.Length];

            for (int i = 0; i < fields.Length; i++)
            {
                item.Fields[i].Info = fields[i];

                // Quick optimization: If this type is the same, then use the same item.
                if (fields[i].FieldType == type) item.Fields[i].Map = item;
                else item.Fields[i].Map = GenerateNonGeneric(settings, type);
            }

            return item;
        }
    }
}
