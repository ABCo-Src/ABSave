using ABSoftware.ABSave.Exceptions;
using ABSoftware.ABSave.Mapping.Description;
using ABSoftware.ABSave.Mapping.Description.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ABSoftware.ABSave.Mapping
{
    public static class KeyInheritanceHandler
    {
        public static string GetOrAddTypeKeyFromCache(Type baseType, Type type, SaveInheritanceAttribute info)
        {
            lock (info)
            {
                // Try to get it from the cache.
                if (info.KeySerializeCache != null && info.KeySerializeCache.TryGetValue(type, out string? val))
                    return val;

                Debug.Assert(!info.HasGeneratedFullKeyCache);

                // If it's not in the cache, get and add it now.
                var attribute = type.GetCustomAttribute<SaveInheritanceKeyAttribute>(false);
                if (attribute == null) throw new UnsupportedSubTypeException(baseType, type);

                info.KeySerializeCache ??= new Dictionary<Type, string>(1);
                info.KeySerializeCache.Add(type, attribute.Key);

                info.KeyDeserializeCache ??= new Dictionary<string, Type>(1);
                info.KeyDeserializeCache.Add(attribute.Key, type);

                return attribute.Key;
            }
        }

        public static void EnsureCreatedDeserializeCacheOnInfo(Type type, SaveInheritanceAttribute info)
        {
            lock (info)
            {
                // Just use one of them to see if the cache is valid or not.
                if (info.HasGeneratedFullKeyCache) return;

                var keyedInfo = GetKeyedSubTypesFor(type);

                // We'll also fill in the serialize cache since we've now gone through all the types.
                info.KeySerializeCache = new Dictionary<Type, string>(keyedInfo.Length);
                info.KeyDeserializeCache = new Dictionary<string, Type>(keyedInfo.Length);

                for (int i = 0; i < keyedInfo.Length; i++)
                {
                    var currentInfo = keyedInfo[i];

                    info.KeySerializeCache.Add(currentInfo.Type, currentInfo.Key);
                    info.KeyDeserializeCache.Add(currentInfo.Key, currentInfo.Type);
                }

                info.HasGeneratedFullKeyCache = true;
            }
        }

        struct KeyedSubTypeInfo
        {
            public Type Type;
            public string Key;

            public KeyedSubTypeInfo(Type type, string key) =>
                (Type, Key) = (type, key);
        }

        static KeyedSubTypeInfo[] GetKeyedSubTypesFor(Type type)
        {
            var typeAssemblyName = type.Assembly.GetName();
            var currentAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            List<KeyedSubTypeInfo> res = new List<KeyedSubTypeInfo>();

            for (int i = 0; i < currentAssemblies.Length; i++)
            {
                var referenced = currentAssemblies[i].GetReferencedAssemblies();
                if (currentAssemblies[i] == type.Assembly || referenced.Contains(typeAssemblyName))
                {
                    var subTypes = currentAssemblies[i].GetTypes();

                    Parallel.ForEach(subTypes, t =>
                    {
                        if (!t.IsSubclassOf(type)) return;

                        var attribute = t.GetCustomAttribute<SaveInheritanceKeyAttribute>(false);
                        if (attribute == null) return;

                        var newInfo = new KeyedSubTypeInfo(t, attribute.Key);

                        // Since it's EXTREMELY unlikely a type will be a sub-class (say 5 / 1000 types),
                        // we don't mind the synchronization as it should be very uncommon.
                        lock (res)
                            res.Add(newInfo);
                    });
                }
            }

            return res.ToArray();
        }
    }
}