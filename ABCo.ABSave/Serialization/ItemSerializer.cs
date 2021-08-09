using ABCo.ABSave.Converters;
using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description;
using ABCo.ABSave.Mapping.Description.Attributes;
using ABCo.ABSave.Mapping.Generation.Inheritance;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Serialization
{
    internal static class ItemSerializer
    {
        public static void SerializeItem(object? obj, MapItemInfo info, BitWriter header)
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

        public static void SerializeExactNonNullItem(object obj, MapItemInfo info, BitWriter header) =>
            SerializeItemNoSetup(obj, info, header, true);

        static void SerializeItemNoSetup(object obj, MapItemInfo info, BitWriter header, bool skipHeader)
        {
            Converter item = info.Converter;
            ABSaveUtils.WaitUntilNotGenerating(item);

            SerializeConverter(obj, info.Converter, header, skipHeader);
        }

        static void SerializeConverter(object obj, Converter converter, BitWriter header, bool skipHeader)
        {
            Type? actualType = obj.GetType();

            bool appliedHeader = true;

            // Write the null and inheritance bits.
            bool sameType = true;
            if (!converter.IsValueItemType && !skipHeader)
            {
                sameType = WriteHeaderNullAndInheritance(actualType, converter, header);
                appliedHeader = false;
            }

            // Write and get the info for a version, if necessary
            if (HandleVersionNumber(converter, out VersionInfo info, header))
                appliedHeader = true;

            // Handle inheritance if needed.
            if (info._inheritanceInfo != null && !sameType)
            {
                SerializeActualType(obj, info._inheritanceInfo, converter.ItemType, actualType, header);
                return;
            }

            // Apply the header if it's not being used and hasn't already been applied.
            if (!info.UsesHeader && !appliedHeader)
                header.MoveToNextByte();

            var serializeInfo = new Converter.SerializeInfo(obj, actualType, info);
            converter.Serialize(in serializeInfo, header);
        }

        // Returns: Whether the type has changed.
        static bool WriteHeaderNullAndInheritance(Type actualType, Converter item, BitWriter target)
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
        internal static bool HandleVersionNumber(Converter item, out VersionInfo info, BitWriter header)
        {
            // If the version has already been written, do nothing
            VersionInfo? existingInfo = header.State.GetExistingVersionInfo(item);
            if (existingInfo != null)
            {
                info = existingInfo;
                return false;
            }

            uint version = header.State.Settings.IncludeVersioning ? WriteNewVersionInfo(item, header) : 0;
            info = header.State.GetNewVersionInfo(item, version);

            return header.State.Settings.IncludeVersioning;
        }

        static uint WriteNewVersionInfo(Converter item, BitWriter target)
        {
            uint targetVersion = 0;

            // Try to get the custom target version and if there is none use the latest.
            if (target.State.TargetVersions?.TryGetValue(item.ItemType, out targetVersion) != true)
                targetVersion = item.HighestVersion;

            target.WriteCompressedInt(targetVersion);
            return targetVersion;
        }

        // Returns: Whether the sub-type was converted in here and we should return now.
        static void SerializeActualType(object obj, SaveInheritanceAttribute info, Type baseType, Type actualType, BitWriter header)
        {
            switch (info.Mode)
            {
                case SaveInheritanceMode.Index:
                    if (!TryWriteListInheritance(info, actualType, false, header))
                        throw new UnsupportedSubTypeException(baseType, actualType);

                    break;
                case SaveInheritanceMode.Key:
                    WriteKeyInheritance(info, baseType, actualType, header);

                    break;
                case SaveInheritanceMode.IndexOrKey:
                    if (!TryWriteListInheritance(info, actualType, true, header))
                    {
                        header.WriteBitOff();
                        WriteKeyInheritance(info, baseType, actualType, header);
                    }

                    break;
            }

            // Serialize the actual type now.
            SerializeItemNoSetup(obj, header.State.GetRuntimeMapItem(actualType), header, true);
        }

        static bool TryWriteListInheritance(SaveInheritanceAttribute info, Type actualType, bool writeOnIfSuccessful, BitWriter header)
        {
            if (info.IndexSerializeCache!.TryGetValue(actualType, out uint pos))
            {
                if (writeOnIfSuccessful) header.WriteBitOn();
                header.WriteCompressedInt(pos);
                return true;
            }

            return false;
        }

        static void WriteKeyInheritance(SaveInheritanceAttribute info, Type baseType, Type actualType, BitWriter header)
        {
            string key = KeyInheritanceHandler.GetOrAddTypeKeyFromCache(baseType, actualType, info);
            header.WriteNonNullString(key);
        }
    }
}
