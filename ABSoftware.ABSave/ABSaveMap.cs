using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace ABSoftware.ABSave.Mapping
{
    public class ABSaveMap
    {
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
            var res = LightConcurrentPool<MapGenerator>.TryRent() ?? new MapGenerator();
            res.Initialize(this);
            return res;
        }

        internal void ReleaseGenerator(MapGenerator gen)
        {
            LightConcurrentPool<MapGenerator>.Release(gen);
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
