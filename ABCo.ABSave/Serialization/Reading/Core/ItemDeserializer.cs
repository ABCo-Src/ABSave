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
        public static object? DeserializeItem(MapItemInfo info, BitReader header)
        {
            // Null check
            if (!info.IsValueTypeItem || info.IsNullable)
            {
                if (!header.ReadBit()) return null;
            }

            return DeserializeItemNoSetup(info, header, info.IsNullable);
        }

        public static object DeserializeExactNonNullItem(MapItemInfo info, BitReader header) =>
            DeserializeItemNoSetup(info, header, true);

        static object DeserializeItemNoSetup(MapItemInfo info, BitReader header, bool skipHeader)
        {
            Converter item = info.Converter;
            ABSaveUtils.WaitUntilNotGenerating(item);

            return DeserializeConverter(info.Converter, header, skipHeader);
        }

        static object DeserializeConverter(Converter converter, BitReader header, bool skipHeader)
        {
            object? inheritanceHandled = DeserializeConverterHeader(converter, header, skipHeader, out var info);
            if (inheritanceHandled != null) return inheritanceHandled;

            var deserializeInfo = new Converter.DeserializeInfo(converter.ItemType, info, header);
            return converter.Deserialize(in deserializeInfo);
        }

        internal static object? DeserializeConverterHeader(Converter converter, BitReader header, bool skipHeader, out VersionInfo info)
        {
            var details = header.State.GetCachedDetails(converter);

            // Handle the inheritance bit.
            bool sameType = true;
            if (!skipHeader)
                sameType = ReadHeader(converter, header);

            // Read or create the version info if needed
            HandleVersionNumber(converter, ref details.CurrentInfo, header);

            // Handle inheritance.
            if (details.CurrentInfo._inheritanceInfo != null && !sameType)
            {
                info = null!;
                return DeserializeActualType(details.CurrentInfo._inheritanceInfo, converter.ItemType, header);
            }

            info = details.CurrentInfo;
            return null;
        }

        static void HandleVersionNumber(Converter converter, ref VersionInfo item, BitReader header)
        {
            // If the version has already been read, do nothing
            if (item != null) return;

            item = header.State.Settings.IncludeVersioning ?
                header.State.CreateNewCache(converter, ReadNewVersionInfo(header)) :
                header.State.Map.GetVersionInfo(converter, 0);
        }

        static bool ReadHeader(Converter item, BitReader header)
        {
            if (item.IsValueItemType) return false;

            // Type
            return header.ReadBit();
        }

        static uint ReadNewVersionInfo(BitReader header) => header.ReadCompressedInt();

        // Returns: Whether the sub-type was converted in here and we should return now.
        static object DeserializeActualType(SaveInheritanceAttribute info, Type baseType, BitReader header)
        {
            Type? actualType = info.Mode switch
            {
                SaveInheritanceMode.Index => TryReadListInheritance(info, baseType, header),
                SaveInheritanceMode.Key => TryReadKeyInheritance(info, baseType, header),
                SaveInheritanceMode.IndexOrKey => header.ReadBit() ? TryReadListInheritance(info, baseType, header) : TryReadKeyInheritance(info, baseType, header),
                _ => throw new Exception("Invalid save inheritance mode")
            };

            if (actualType == null) throw new InvalidSubTypeInfoException(baseType);

            // Deserialize the actual type.
            return DeserializeItemNoSetup(header.State.GetRuntimeMapItem(actualType), header, true);
        }

        static Type? TryReadListInheritance(SaveInheritanceAttribute info, Type baseType, BitReader header)
        {
            uint key = header.ReadCompressedInt();
            return info.IndexDeserializeCache.GetValueOrDefault(key);
        }

        static Type? TryReadKeyInheritance(SaveInheritanceAttribute info, Type baseType, BitReader header)
        {
            // If it's cached, use the cache.
            if (header.ReadBit())
            {
                int key = (int)header.ReadCompressedInt();
                return header.State.CachedKeys[key];
            }
            else
            {
                // Make sure the info is initialized for deserialization.
                KeyInheritanceHandler.EnsureHasAllTypeCache(baseType, info);

                // Read in the key from the source.
                string key = header.ReadNonNullString();

                // See if there's an item with that key.
                return info.KeyDeserializeCache!.GetValueOrDefault(key);
            }
        }
    }
}
