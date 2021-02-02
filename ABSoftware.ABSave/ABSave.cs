using ABSoftware.ABSave.Deserialization;
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
    public static class ABSave
    {
        public static byte[] Serialize<T>(T obj, ABSaveMap map) => SerializeNonGeneric(obj, map);
        public static byte[] SerializeNonGeneric(object obj, ABSaveMap map)
        {
            var stream = new MemoryStream(); // Use pooling for "MemoryStream"s?
            SerializeNonGeneric(obj, map, stream);
            return stream.ToArray();
        }

        public static void Serialize<T>(T obj, ABSaveMap map, Stream stream) => Serialize(obj, map, stream);
        public static void SerializeNonGeneric(object obj, ABSaveMap map, Stream stream)
        {
            var serializer = new ABSaveSerializer(stream, map);
            serializer.SerializeRoot(obj);
        }

        public static T Deserialize<T>(byte[] arr, ABSaveMap map) => (T)DeserializeNonGeneric(arr, map);
        public static object DeserializeNonGeneric(byte[] arr, ABSaveMap map)
        {
            var stream = new MemoryStream(arr);
            return DeserializeNonGeneric(stream, map);
        }

        public static T Deserialize<T>(Stream stream, ABSaveMap map) => (T)DeserializeNonGeneric(stream, map);
        public static object DeserializeNonGeneric(Stream stream, ABSaveMap map)
        {
            var serializer = new ABSaveDeserializer(stream, map);
            return serializer.DeserializeRoot();
        }
    }
}
