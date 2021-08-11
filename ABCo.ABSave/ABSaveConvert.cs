using ABCo.ABSave.Serialization.Reading;
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
        public static byte[] Serialize<T>(T obj, ABSaveMap map, bool writeVersioning = false, Dictionary<Type, uint>? targetVersions = null) => SerializeNonGeneric(obj, map, writeVersioning, targetVersions);
        public static byte[] SerializeNonGeneric(object? obj, ABSaveMap map, bool writeVersioning = false, Dictionary<Type, uint>? targetVersions = null)
        {
            var stream = new MemoryStream(); // Use pooling for "MemoryStream"s?
            SerializeNonGeneric(obj, map, stream, writeVersioning, targetVersions);
            return stream.ToArray();
        }

        public static void Serialize<T>(T obj, ABSaveMap map, Stream stream, bool writeVersioning = false, Dictionary<Type, uint>? targetVersions = null) =>
            SerializeNonGeneric(obj, map, stream, writeVersioning, targetVersions);
        public static void SerializeNonGeneric(object? obj, ABSaveMap map, Stream stream, bool writeVersioning = false, Dictionary<Type, uint>? targetVersions = null)
        {
            using ABSaveSerializer serializer = map.GetSerializer(stream, writeVersioning, targetVersions);
            using BitWriter header = serializer.GetHeader();
            header.WriteSettingsHeaderIfNeeded();
            header.WriteRoot(obj);
        }

        public static T Deserialize<T>(byte[] arr, ABSaveMap map, bool? writeVersioning = null) =>
            (T)DeserializeNonGeneric(arr, map, writeVersioning)!;

        public static object? DeserializeNonGeneric(byte[] arr, ABSaveMap map, bool? writeVersioning = null)
        {
            var stream = new MemoryStream(arr);
            return DeserializeNonGeneric(stream, map, writeVersioning);
        }

        public static T Deserialize<T>(Stream stream, ABSaveMap map, bool? writeVersioning = null) where T : class? =>
            (T)DeserializeNonGeneric(stream, map, writeVersioning)!;

        public static object? DeserializeNonGeneric(Stream stream, ABSaveMap map, bool? writeVersioning = null)
        {
            using ABSaveDeserializer deserializer = map.GetDeserializer(stream, writeVersioning);
            BitReader header = deserializer.GetHeader();
            header.ReadSettingsHeaderIfNeeded();
            return header.ReadRoot();
        }
    }
}
