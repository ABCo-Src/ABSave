using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Exceptions;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping.Generation;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace ABSoftware.ABSave.Mapping
{
    public class ABSaveMap
    {
        internal LightConcurrentPool<MapGenerator> GeneratorPool = new LightConcurrentPool<MapGenerator>(4);
        internal NonReallocatingList<MapItem> Items;
        internal MapItemInfo RootItem;
        internal MapItemInfo AssemblyItem;
        internal MapItemInfo VersionItem;
        internal MapGenerationInfo GenInfo;

        /// <summary>
        /// The configuration this map uses.
        /// </summary>
        public ABSaveSettings Settings { get; set; }

        /// <summary>
        /// Stores all sub-types that may be found inside this map and its items. These are found at serialization-time when there is inheritance type differences.
        /// </summary>
        //internal Dictionary<Type, RuntimeMapItem> CachedSubItems { get; set; } = new Dictionary<Type, RuntimeMapItem>();

        // Internal for use with unit tests.
        internal ABSaveMap(ABSaveSettings settings)
        {
            Settings = settings;
            Items.Initialize();
            GenInfo.AllTypes = new Dictionary<Type, GenMapItemInfo>();
            GenInfo.RuntimeMapItems = new Dictionary<Type, GenMapItemInfo>();
        }

        public static ABSaveMap Get<T>(ABSaveSettings settings) => GetNonGeneric(typeof(T), settings);

        public static ABSaveMap GetNonGeneric(Type type, ABSaveSettings settings)
        {
            var map = new ABSaveMap(settings);
            var generator = map.RentGenerator();

            map.RootItem = generator.GetMap(type);
            map.AssemblyItem = generator.GetMap(typeof(Assembly));
            map.VersionItem = generator.GetMap(typeof(Version));

            map.ReleaseGenerator(generator);
            return map;
        }

        internal MapGenerator RentGenerator()
        {
            var res = GeneratorPool.TryRent() ?? new MapGenerator();
            res.Initialize(this);
            return res;
        }

        internal void ReleaseGenerator(MapGenerator gen)
        {
            GeneratorPool.Release(gen);
        }

        internal ObjectMemberInfo[] GetMembersForVersion(ref MapItem item, int version)
        {
            ref ObjectMapItem obj = ref item.Main.Object;

            // Try to get the version if it already exists.
            var existing = ObjectMapper.GetVersionOrAddNull(version, ref obj);
            if (existing != null) return existing;

            // If it doesn't, generate it.
            {
                var gen = RentGenerator();
                var res = ObjectMapper.GenerateVersion(gen, version, ref item, ref obj);
                ReleaseGenerator(gen);

                return res;
            }
        }

        public Type GetTypeOf(MapItemInfo info) => Items.GetItemRef(info.Pos).ItemType;

        internal ref MapItem GetItemAt(MapItemInfo info) => ref Items.GetItemRef(info.Pos);
    }

    internal struct MapGenerationInfo
    {
        /// <summary>
        /// All the types present throughout the map, and their respective map item.
        /// </summary>
        public Dictionary<Type, GenMapItemInfo> AllTypes;

        /// <summary>
        /// All the types that were loaded at runtime, and their respective "runtime" map item.
        /// </summary>
        public Dictionary<Type, GenMapItemInfo> RuntimeMapItems;
    }
}
