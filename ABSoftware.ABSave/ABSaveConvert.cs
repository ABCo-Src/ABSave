using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ABSoftware.ABSave
{
    /// <summary>
    /// Converts to and from ABSave.
    /// </summary>
    public static class ABSaveConvert
    {
        readonly static LightConcurrentPool<ABSaveSerializer> SerializerPool = new LightConcurrentPool<ABSaveSerializer>(2);
        readonly static LightConcurrentPool<ABSaveDeserializer> DeserializerPool = new LightConcurrentPool<ABSaveDeserializer>(2);
        public static byte[] Serialize<T>(T obj, ABSaveMap map, Dictionary<Type, uint>? targetVersions = null) => SerializeNonGeneric(obj, map, targetVersions);
        public static byte[] SerializeNonGeneric(object? obj, ABSaveMap map, Dictionary<Type, uint>? targetVersions = null)
        {
            var stream = new MemoryStream(); // Use pooling for "MemoryStream"s?
            SerializeNonGeneric(obj, map, stream, targetVersions);
            return stream.ToArray();
        }

        public static void Serialize<T>(T obj, ABSaveMap map, Stream stream, Dictionary<Type, uint>? targetVersions = null) => 
            SerializeNonGeneric(obj, map, stream, targetVersions);
        public static void SerializeNonGeneric(object? obj, ABSaveMap map, Stream stream, Dictionary<Type, uint>? targetVersions = null)
        {
            var serializer = SerializerPool.TryRent() ?? new ABSaveSerializer();
            serializer.Initialize(stream, map, targetVersions);
            serializer.SerializeRoot(obj);
            SerializerPool.Release(serializer);
        }

        public static T Deserialize<T>(byte[] arr, ABSaveMap map) => 
            (T)DeserializeNonGeneric(arr, map)!;

        public static object? DeserializeNonGeneric(byte[] arr, ABSaveMap map)
        {
            var stream = new MemoryStream(arr);
            return DeserializeNonGeneric(stream, map);
        }

        public static T Deserialize<T>(Stream stream, ABSaveMap map) where T : class? => 
            (T)DeserializeNonGeneric(stream, map)!;

        public static object? DeserializeNonGeneric(Stream stream, ABSaveMap map)
        {
            var deserializer = DeserializerPool.TryRent() ?? new ABSaveDeserializer();
            deserializer.Initialize(stream, map);
            return deserializer.DeserializeRoot();
        }
    }
}
