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

        /// <summary>
        /// All the types present throughout the map, and their respective map item.
        /// </summary>
        internal Dictionary<Type, MapItem?> AllTypes;

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

            map.ReleaseGenerator(generator);
            return map;
        }

        internal ObjectMemberSharedInfo[] GetMembersForVersion(ObjectMapItem item, uint version)
        {
            // Try to get the version if it already exists.
            var existing = MapGenerator.GetVersionOrAddNull(version, item);
            if (existing != null) return existing;

            // If it doesn't, generate it.
            var gen = GetGenerator();
            var res = gen.GenerateVersion(version, item);
            ReleaseGenerator(gen);
            return res;
        }

        internal MapItemInfo GetRuntimeMapItem(Type type)
        {
            var gen = GetGenerator();
            var map = gen.GetRuntimeMap(type);
            ReleaseGenerator(gen);
            return map;
        }

        internal MapGenerator GetGenerator()
        {
            MapGenerator res = GeneratorPool.Value!;
            res.Initialize(this);
            return res;
        }

        // NOTE: It's generally not a good idea to call this from a "finally" just in case it pools
        // a generator that's been left in an invalid state.
        internal void ReleaseGenerator(MapGenerator gen)
        {
            gen.FinishGeneration();
        }
    }
}
