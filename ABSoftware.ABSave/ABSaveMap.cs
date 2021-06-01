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
        internal static ThreadLocal<MapGenerator> GeneratorPool = new ThreadLocal<MapGenerator>(() => new MapGenerator());
        internal MapItemInfo RootItem;
        internal MapItemInfo AssemblyItem;
        internal MapItemInfo VersionItem;
        internal MapGenerationInfo GenInfo;

        /// <summary>
        /// All the types present throughout the map, and their respective map item.
        /// </summary>
        public Dictionary<Type, MapItem?> AllTypes;

        /// <summary>
        /// The configuration this map uses.
        /// </summary>
        public ABSaveSettings Settings { get; set; }

        // Internal for use with unit tests.
        internal ABSaveMap(ABSaveSettings settings)
        {
            Settings = settings;
            AllTypes = new Dictionary<Type, MapItem?>();
        }

        public static ABSaveMap Get<T>(ABSaveSettings settings) => GetNonGeneric(typeof(T), settings);

        public static ABSaveMap GetNonGeneric(Type type, ABSaveSettings settings)
        {
            var map = new ABSaveMap(settings);
            var generator = map.GetGenerator();

            map.RootItem = generator.GetMap(type);
            map.AssemblyItem = generator.GetMap(typeof(Assembly));
            map.VersionItem = generator.GetMap(typeof(Version));
            return map;
        }

        internal MapGenerator GetGenerator()
        {
            MapGenerator res = GeneratorPool.Value!;
            res.Initialize(this);
            return res;
        }

        internal ObjectMemberSharedInfo[] GetMembersForVersion(ObjectMapItem item, uint version)
        {
            // Try to get the version if it already exists.
            var existing = MapGenerator.GetVersionOrAddNull(version, item);
            if (existing != null) return existing;

            // If it doesn't, generate it.
            return GetGenerator().GenerateVersion(version, item);
        }

        internal MapItemInfo GetRuntimeMapItem(Type type) => GetGenerator().GetRuntimeMap(type);
    }

    internal struct MapGenerationInfo
    {
    }
}
