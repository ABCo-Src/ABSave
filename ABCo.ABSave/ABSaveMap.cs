using ABCo.ABSave.Configuration;
using ABCo.ABSave.Serialization.Converters;
using ABCo.ABSave.Serialization.Writing.Reading;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping.Generation;
using ABCo.ABSave.Mapping.Generation.General;
using ABCo.ABSave.Serialization.Writing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ABCo.ABSave.Mapping;

namespace ABCo.ABSave
{
    public class ABSaveMap
    {
        internal static ThreadLocal<MapGenerator> _generatorPool = new ThreadLocal<MapGenerator>(() => new MapGenerator());
        internal MapItemInfo _rootItem;
        internal uint _highestConverterInstanceId;

        readonly LightConcurrentPool<ABSaveSerializer> _serializerPool = new LightConcurrentPool<ABSaveSerializer>(2);
        readonly LightConcurrentPool<ABSaveDeserializer> _deserializerPool = new LightConcurrentPool<ABSaveDeserializer>(2);

        /// <summary>
        /// All the types present throughout the map, and their respective map item.
        /// </summary>
        internal Dictionary<Type, Converter?> _allTypes;

        /// <summary>
        /// The configuration this map uses.
        /// </summary>
        public ABSaveSettings Settings { get; set; }

        // Internal for use with unit tests.
        internal ABSaveMap(ABSaveSettings settings)
        {
            Settings = settings;
            _allTypes = new Dictionary<Type, Converter?>();
        }

        public static ABSaveMap Get<T>(ABSaveSettings settings) => GetNonGeneric(typeof(T), settings);

        public static ABSaveMap GetNonGeneric(Type type, ABSaveSettings settings)
        {
            var map = new ABSaveMap(settings);
            MapGenerator? generator = map.GetGenerator();

            map._rootItem = generator.GetMap(type);
            ReleaseGenerator(generator);
            return map;
        }

        internal VersionInfo GetVersionInfo(Converter converter, uint version)
        {
            // Try to get the version if it already exists.
            VersionInfo? existing = VersionCacheHandler.GetVersionOrAddNull(converter, version);
            if (existing != null) return existing;

            // If it doesn't, generate it.
            MapGenerator? gen = GetGenerator();
            VersionInfo? res = VersionCacheHandler.AddNewVersion(converter, version, gen);
            ReleaseGenerator(gen);
            return res;
        }

        internal MapItemInfo GetRuntimeMapItem(Type type)
        {
            MapGenerator? gen = GetGenerator();
            MapItemInfo map = gen.GetRuntimeMap(type);
            ReleaseGenerator(gen);
            return map;
        }

        #region Generator Pooling

        internal MapGenerator GetGenerator()
        {
            MapGenerator res = _generatorPool.Value!;
            res.Initialize(this);
            return res;
        }

        // NOTE: It's generally not a good idea to call this from a "finally" just in case it pools
        // a generator that's been left in an invalid state.
        internal static void ReleaseGenerator(MapGenerator gen) => gen.FinishGeneration();

        #endregion

        #region Serializer Pooling

        public ABSaveSerializer GetSerializer(Stream destStream, Dictionary<Type, uint>? targetVersions = null)
        {
            ABSaveSerializer serializer = _serializerPool.TryRent() ?? new ABSaveSerializer(this);
            serializer.Initialize(destStream, targetVersions);
            return serializer;
        }

        internal void ReleaseSerializer(ABSaveSerializer serializer) => _serializerPool.Release(serializer);

        #endregion

        #region Deserializer Pooling

        public ABSaveDeserializer GetDeserializer(Stream destStream)
        {
            ABSaveDeserializer deserializer = _deserializerPool.TryRent() ?? new ABSaveDeserializer(this);
            deserializer.Initialize(destStream);
            return deserializer;
        }

        internal void ReleaseDeserializer(ABSaveDeserializer serializer) => _deserializerPool.Release(serializer);

        #endregion
    }
}
