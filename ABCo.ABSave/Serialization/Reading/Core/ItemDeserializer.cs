using ABCo.ABSave.Serialization.Converters;
using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description;
using ABCo.ABSave.Mapping.Description.Attributes;
using ABCo.ABSave.Mapping.Generation.Inheritance;
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace ABCo.ABSave.Serialization.Reading.Core
{
    internal static class ItemDeserializer
    {
        public static object? DeserializeItem(MapItemInfo info, ABSaveDeserializer deserializer)
        {
            // Null check
            if (!info.IsValueTypeItem || info.IsNullable)
            {
                if (!deserializer.ReadBit()) return null;
            }

            return DeserializeItemNoSetup(info.Converter, deserializer, info.IsNullable);
        }

        public static object DeserializeExactNonNullItem(MapItemInfo info, ABSaveDeserializer deserializer) =>
            DeserializeItemNoSetup(info.Converter, deserializer, true);

        static object DeserializeItemNoSetup(Converter info, ABSaveDeserializer deserializer, bool skipHeader)
        {
            ABSaveUtils.WaitUntilNotGenerating(info);
            return DeserializeConverter(info, deserializer, skipHeader);
        }

        static object DeserializeConverter(Converter converter, ABSaveDeserializer deserializer, bool skipHeader)
        {
            object? inheritanceHandled = DeserializeVersionInfoAndHeader(converter, deserializer, skipHeader, out var info);
            if (inheritanceHandled != null) return inheritanceHandled;

            var deserializeInfo = new Converter.DeserializeInfo(converter.ItemType, info, deserializer);
            return converter.Deserialize(in deserializeInfo);
        }

        internal static object? DeserializeVersionInfoAndHeader(Converter converter, ABSaveDeserializer deserializer, bool skipHeader, out VersionInfo info)
        {
            var details = deserializer.State.GetCachedInfo(converter);

            // Read or get the version info, if needed
            if (details == null)
                details = HandleNewVersion(converter, deserializer);

            if (details._inheritanceInfo != null && !skipHeader)
            {
                info = null!;
                return DeserializeActualType(details._inheritanceInfo, converter, deserializer);
            }

            info = details;
            return null;
        }

        static VersionInfo HandleNewVersion(Converter converter, ABSaveDeserializer deserializer) =>
            deserializer.State.IncludeVersioningInfo ?
                deserializer.State.CreateNewCache(converter, ReadNewVersionInfo(deserializer)) :
                deserializer.State.Map.GetVersionInfo(converter, 0);

        static uint ReadNewVersionInfo(ABSaveDeserializer header) => header.ReadCompressedInt();

        static object? DeserializeActualType(SaveInheritanceAttribute info, Converter baseConverter, ABSaveDeserializer deserializer)
        {
            // If it's the same type, just deserialize that same type.
            if (deserializer.ReadBit())
                return DeserializeItemNoSetup(baseConverter, deserializer, true);

            Type? actualType = info.Mode switch
            {
                SaveInheritanceMode.Index => TryReadListInheritance(info, deserializer),
                SaveInheritanceMode.Key => TryReadKeyInheritance(info, baseConverter.ItemType, deserializer),
                SaveInheritanceMode.IndexOrKey => deserializer.ReadBit() ? TryReadListInheritance(info, deserializer) : TryReadKeyInheritance(info, baseConverter.ItemType, deserializer),
                _ => throw new Exception("Invalid save inheritance mode")
            };

            if (actualType == null) throw new InvalidSubTypeInfoException(baseConverter.ItemType);

            // Deserialize the actual type.
            return DeserializeItemNoSetup(deserializer.State.GetRuntimeMapItem(actualType).Converter, deserializer, true);
        }

        static Type? TryReadListInheritance(SaveInheritanceAttribute info, ABSaveDeserializer deserializer)
        {
            uint key = deserializer.ReadCompressedInt();
            return info.IndexDeserializeCache.GetValueOrDefault(key);
        }

        static Type? TryReadKeyInheritance(SaveInheritanceAttribute info, Type baseType, ABSaveDeserializer deserializer)
        {
            // If it's cached, use the cache.
            if (deserializer.ReadBit())
            {
                int key = (int)deserializer.ReadCompressedInt();
                return deserializer.State.CachedKeys[key];
            }
            else
            {
                // Make sure the info is initialized for deserialization.
                KeyInheritanceHandler.EnsureHasAllTypeCache(baseType, info);

                // Read in the key from the source.
                string key = deserializer.ReadNonNullString();

                // See if there's an item with that key.
                Type? ret = info.KeyDeserializeCache!.GetValueOrDefault(key);

                // Add the item to the cache, if there is one.
                if (ret != null) deserializer.State.CachedKeys.Add(ret);
                return ret;
            }
        }
    }
}
