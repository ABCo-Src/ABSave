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

            return DeserializeItemNoSetup(info, deserializer, info.IsNullable);
        }

        public static object DeserializeExactNonNullItem(MapItemInfo info, ABSaveDeserializer deserializer) =>
            DeserializeItemNoSetup(info, deserializer, true);

        static object DeserializeItemNoSetup(MapItemInfo info, ABSaveDeserializer deserializer, bool skipHeader)
        {
            Converter item = info.Converter;
            ABSaveUtils.WaitUntilNotGenerating(item);

            return DeserializeConverter(info.Converter, deserializer, skipHeader);
        }

        static object DeserializeConverter(Converter converter, ABSaveDeserializer deserializer, bool skipHeader)
        {
            object? inheritanceHandled = DeserializeConverterHeader(converter, deserializer, skipHeader, out var info);
            if (inheritanceHandled != null) return inheritanceHandled;

            var deserializeInfo = new Converter.DeserializeInfo(converter.ItemType, info, deserializer);
            return converter.Deserialize(in deserializeInfo);
        }

        internal static object? DeserializeConverterHeader(Converter converter, ABSaveDeserializer deserializer, bool skipHeader, out VersionInfo info)
        {
            var details = deserializer.State.GetCachedInfo(converter);

            // Handle the inheritance bit.
            bool sameType = true;
            if (!skipHeader)
                sameType = ReadHeader(converter, deserializer);

            // Read or create the version info if needed
            HandleVersionNumber(converter, ref details, deserializer);

            // Handle inheritance.
            if (details._inheritanceInfo != null && !sameType)
            {
                info = null!;
                return DeserializeActualType(details._inheritanceInfo, converter.ItemType, deserializer);
            }

            info = details;
            return null;
        }

        static void HandleVersionNumber(Converter converter, ref VersionInfo item, ABSaveDeserializer deserializer)
        {
            // If the version has already been read, do nothing
            if (item != null) return;

            item = deserializer.State.HasVersioningInfo ?
                deserializer.State.CreateNewCache(converter, ReadNewVersionInfo(deserializer)) :
                deserializer.State.Map.GetVersionInfo(converter, 0);
        }

        static bool ReadHeader(Converter item, ABSaveDeserializer header)
        {
            if (item.IsValueItemType) return false;

            // Type
            return header.ReadBit();
        }

        static uint ReadNewVersionInfo(ABSaveDeserializer header) => header.ReadCompressedInt();

        // Returns: Whether the sub-type was converted in here and we should return now.
        static object DeserializeActualType(SaveInheritanceAttribute info, Type baseType, ABSaveDeserializer deserializer)
        {
            Type? actualType = info.Mode switch
            {
                SaveInheritanceMode.Index => TryReadListInheritance(info, baseType, deserializer),
                SaveInheritanceMode.Key => TryReadKeyInheritance(info, baseType, deserializer),
                SaveInheritanceMode.IndexOrKey => deserializer.ReadBit() ? TryReadListInheritance(info, baseType, deserializer) : TryReadKeyInheritance(info, baseType, deserializer),
                _ => throw new Exception("Invalid save inheritance mode")
            };

            if (actualType == null) throw new InvalidSubTypeInfoException(baseType);

            // Deserialize the actual type.
            return DeserializeItemNoSetup(deserializer.State.GetRuntimeMapItem(actualType), deserializer, true);
        }

        static Type? TryReadListInheritance(SaveInheritanceAttribute info, Type baseType, ABSaveDeserializer deserializer)
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
