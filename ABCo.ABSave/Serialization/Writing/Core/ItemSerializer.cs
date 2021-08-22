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
        public static void SerializeItem(object? obj, MapItemInfo info, ABSaveSerializer header)
        {
            // Say it's "not null" if it is nullable.
            if (obj == null)
                header.WriteBitOff();
            else
            {
                if (info.IsNullable) header.WriteBitOn();
                SerializeItemNoSetup(obj, info, header, info.IsNullable);
            }
        }

        public static void SerializeExactNonNullItem(object obj, MapItemInfo info, ABSaveSerializer header) =>
            SerializeItemNoSetup(obj, info, header, true);

        static void SerializeItemNoSetup(object obj, MapItemInfo info, ABSaveSerializer header, bool skipHeader)
        {
            Converter item = info.Converter;
            ABSaveUtils.WaitUntilNotGenerating(item);

            SerializeConverter(obj, info.Converter, header, skipHeader);
        }

        static void SerializeConverter(object obj, Converter converter, ABSaveSerializer header, bool skipHeader)
        {
            Type? actualType = obj.GetType();

            var currentInfo = SerializeConverterHeader(obj, converter, actualType, skipHeader, header);
            if (currentInfo == null) return;

            var serializeInfo = new Converter.SerializeInfo(obj, actualType, currentInfo, header);
            converter.Serialize(in serializeInfo);
        }

        internal static VersionInfo? SerializeConverterHeader(object obj, Converter converter, Type actualType, bool skipHeader, ABSaveSerializer header)
        {
            var cache = header.State.GetCachedInfo(converter);
            bool appliedHeader = true;

            // Write the null and inheritance bits.
            bool sameType = true;
            if (!converter.IsValueItemType && !skipHeader)
            {
                sameType = WriteHeaderNullAndInheritance(actualType, converter, header);
                appliedHeader = false;
            }

            // Write and get the info for a version, if necessary
            if (HandleVersionNumber(converter, ref cache, header))
                appliedHeader = true;

            // Handle inheritance if needed.
            if (cache._inheritanceInfo != null && !sameType)
            {
                SerializeActualType(cache._inheritanceInfo, obj, actualType, converter, header);
                return null;
            }

            // Apply the header if it's not being used and hasn't already been applied.
            if (!cache.UsesHeader && !appliedHeader)
                header.FinishWritingBitsToCurrentByte();

            return cache;
        }

        // Returns: Whether the type has changed.
        static bool WriteHeaderNullAndInheritance(Type actualType, Converter item, ABSaveSerializer target)
        {
            target.WriteBitOn(); // Non-Null

            bool sameType = item.ItemType == actualType;
            target.WriteBitWith(sameType);
            return sameType;
        }

        /// <summary>
        /// Handles the version info for a given converter. If the version hasn't been written yet, it's written now. If not, nothing is written.
        /// </summary>
        /// <returns>Whether we applied the header</returns>
        static bool HandleVersionNumber(Converter item, ref VersionInfo info, ABSaveSerializer header)
        {
            // If the version has already been written (there's info in the cache), do nothing
            if (info != null) return false;

            // If not, write the version and add the converter to the cache.
            uint version = header.State.HasVersioningInfo ? WriteNewVersionInfo(item, header) : 0;
            info = header.State.CreateNewCache(item, version);

            return header.State.HasVersioningInfo;
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

        // Returns: Whether the sub-type was converted in here and we should return now.
        static void SerializeActualType(SaveInheritanceAttribute info, object obj, Type actualType, Converter converter, ABSaveSerializer header)
        {
            var actual = header.State.GetRuntimeMapItem(actualType);
            int? cacheNum = header.State.GetCachedKeyInfo(actual.Converter);

            switch (info.Mode)
            {
                case SaveInheritanceMode.Index:
                    if (!TryWriteListInheritance(info, actualType, false, header))
                        throw new UnsupportedSubTypeException(converter.ItemType, actualType);

                    break;
                case SaveInheritanceMode.Key:
                    WriteKeyInheritance(info, cacheNum, converter, actual.Converter, header);

                    break;
                case SaveInheritanceMode.IndexOrKey:
                    if (!TryWriteListInheritance(info, actualType, true, header))
                    {
                        header.WriteBitOff();
                        WriteKeyInheritance(info, cacheNum, converter, actual.Converter, header);
                    }

                    break;
            }

            // Serialize the actual type now.
            SerializeItemNoSetup(obj, actual, header, true);
        }

        static bool TryWriteListInheritance(SaveInheritanceAttribute info, Type actualType, bool writeOnIfSuccessful, ABSaveSerializer header)
        {
            if (info.IndexSerializeCache!.TryGetValue(actualType, out uint pos))
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
