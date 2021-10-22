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

namespace ABCo.ABSave.Serialization.Writing.Core
{
    internal static class ItemSerializer
    {
        public static void SerializeItem(object? obj, MapItemInfo info, ABSaveSerializer serializer)
        {
            if (obj == null)
                serializer.WriteBitOff();
            else
            {
                // Write "not-null".
                if (!info.Converter.IsValueItemType || info.IsNullable)
                    serializer.WriteBitOn();

                SerializeItemNoSetup(obj, info.Converter, serializer, info.IsNullable);
            }
        }

        public static void SerializeExactNonNullItem(object obj, MapItemInfo info, ABSaveSerializer serializer) =>
            SerializeItemNoSetup(obj, info.Converter, serializer, true);

        static void SerializeItemNoSetup(object obj, Converter info, ABSaveSerializer serializer, bool skipHeader)
        {
            ABSaveUtils.WaitUntilNotGenerating(info);
            SerializeConverter(obj, info, serializer, skipHeader);
        }

        static void SerializeConverter(object obj, Converter converter, ABSaveSerializer serializer, bool skipHeader)
        {
            Type? actualType = obj.GetType();

            var currentInfo = skipHeader ? SerializeVersionInfo(converter, serializer) : SerializeVersionInfoAndHeader(obj, converter, actualType, serializer);
            if (currentInfo == null) return;

            var serializeInfo = new Converter.SerializeInfo(obj, actualType, currentInfo, serializer);
            converter.Serialize(in serializeInfo);
        }

        public static VersionInfo? SerializeVersionInfoAndHeader(object obj, Converter converter, Type actualType, ABSaveSerializer serializer)
        {
            VersionInfo? cache = SerializeVersionInfo(converter, serializer);

            if (cache._inheritanceInfo != null)
            {
                SerializeActualTypeIfNeeded(cache._inheritanceInfo, obj, actualType, converter, serializer);
                return null;
            }

            return cache;
        }

        public static VersionInfo SerializeVersionInfo(Converter converter, ABSaveSerializer header)
        {
            var cache = header.State.GetCachedInfo(converter);

            // Write and get the info for a version, if necessary
            if (cache == null)
                cache = HandleNewVersion(converter, header);

            return cache;
        }

        /// <summary>
        /// Handles the version info for a given converter. If the version hasn't been written yet, it's written now. If not, nothing is written.
        /// </summary>
        /// <returns>Whether we applied the header</returns>
        static VersionInfo HandleNewVersion(Converter item, ABSaveSerializer header)
        {
            uint version = header.State.IncludeVersioningInfo ? WriteNewVersionInfo(item, header) : 0;
            return header.State.CreateNewCache(item, version);
        }

        static uint WriteNewVersionInfo(Converter item, ABSaveSerializer target)
        {
            uint targetVersion = 0;

            // Try to get the custom target version and if there is none use the latest.
            if (target.State.TargetVersions?.TryGetValue(item.ItemType, out targetVersion) != true)
                targetVersion = item.HighestVersion;

            target.WriteCompressedInt(targetVersion);
            return targetVersion;
        }

        static void SerializeActualTypeIfNeeded(SaveInheritanceAttribute info, object obj, Type actualType, Converter converter, ABSaveSerializer serializer)
        {
            bool sameType = converter.ItemType == actualType;
            serializer.WriteBitWith(sameType);

            // If it's the same type, just serialize that same type.
            if (sameType)
            {
                SerializeItemNoSetup(obj, converter, serializer, true);
                return;
            }

            // If not, write inheritance info and serialize the actual type!
            var actual = serializer.State.GetRuntimeMapItem(actualType);
            int? cacheNum = serializer.State.GetCachedKeyInfo(actual.Converter);

            switch (info.Mode)
            {
                case SaveInheritanceMode.Index:
                    if (!TryWriteListInheritance(info, actualType, false, serializer))
                        throw new UnsupportedSubTypeException(converter.ItemType, actualType);

                    break;
                case SaveInheritanceMode.Key:
                    WriteKeyInheritance(info, cacheNum, converter, actual.Converter, serializer);

                    break;
                case SaveInheritanceMode.IndexOrKey:
                    if (!TryWriteListInheritance(info, actualType, true, serializer))
                    {
                        serializer.WriteBitOff();
                        WriteKeyInheritance(info, cacheNum, converter, actual.Converter, serializer);
                    }

                    break;
            }

            // Serialize the actual type now.
            SerializeItemNoSetup(obj, actual.Converter, serializer, true);
        }

        static bool TryWriteListInheritance(SaveInheritanceAttribute info, Type actualType, bool writeOnIfSuccessful, ABSaveSerializer header)
        {
            if (info.IndexSerializeCache.TryGetValue(actualType, out uint pos))
            {
                if (writeOnIfSuccessful) header.WriteBitOn();
                header.WriteCompressedInt(pos);
                return true;
            }

            return false;
        }

        static void WriteKeyInheritance(SaveInheritanceAttribute info, int? cacheNum, Converter converter, Converter actualConverter, ABSaveSerializer header)
        {
            // If the sub-type has been given a cache number already, write that cache number.
            if (cacheNum != null)
            {
                header.WriteBitOn();
                header.WriteCompressedInt((uint)cacheNum);
            }
            else
            {
                header.WriteBitOff();

                string key = KeyInheritanceHandler.GetOrAddTypeKeyFromCache(converter.ItemType, actualConverter.ItemType, info);
                header.WriteNonNullString(key);

                // Cache this sub-type from now on.
                header.State.AddNewKeyCacheNumber(actualConverter);
            }
        }
    }
}
