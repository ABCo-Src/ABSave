using ABCo.ABSave.Serialization.Writing.Reading;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Serialization.Writing;
using System;
using System.Collections.Generic;
using System.IO;

namespace ABCo.ABSave
{
    /// <summary>
    /// Converts to and from ABSave.
    /// </summary>
    public static class ABSaveConvert
    {
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
            using ABSaveSerializer serializer = map.GetSerializer(stream, targetVersions);
            serializer.SerializeRoot(obj);
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
            using ABSaveDeserializer deserializer = map.GetDeserializer(stream);
            return deserializer.DeserializeRoot();
        }
    }
}
