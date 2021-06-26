﻿using ABCo.ABSave.Configuration;
using ABCo.ABSave.Converters;
using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description;
using ABCo.ABSave.Mapping.Description.Attributes;
using ABCo.ABSave.Mapping.Generation.Inheritance;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace ABCo.ABSave.Serialization
{
    /// <summary>
    /// The central object that everything in ABSave writes to. Provides facilties to write primitive types, including strings.
    /// </summary>
    public sealed partial class ABSaveSerializer
    {
        readonly Dictionary<Type, VersionInfo> _versions = new Dictionary<Type, VersionInfo>();

        public Dictionary<Type, uint>? TargetVersions { get; private set; }
        public ABSaveMap Map { get; private set; } = null!;
        public ABSaveSettings Settings { get; private set; } = null!;
        public Stream Output { get; private set; } = null!;
        public bool ShouldReverseEndian { get; private set; }

        byte[]? _stringBuffer;

        public void Initialize(Stream output, ABSaveMap map, Dictionary<Type, uint>? targetVersions)
        {
            if (!output.CanWrite)
                throw new Exception("Cannot use unwriteable stream.");

            Output = output;

            Map = map;
            Settings = map.Settings;
            TargetVersions = targetVersions;

            ShouldReverseEndian = map.Settings.UseLittleEndian != BitConverter.IsLittleEndian;

            Reset();
        }

        public void Reset() => _versions.Clear();

        public MapItemInfo GetRuntimeMapItem(Type type) => Map.GetRuntimeMapItem(type);

        public void SerializeRoot(object? obj) => SerializeItem(obj, Map.RootItem);

        public void SerializeItem(object? obj, MapItemInfo item)
        {
            if (obj == null)
                WriteByte(0);

            else
            {
                var currentHeader = new BitTarget(this);
                SerializePossibleNullableItem(obj, item, ref currentHeader);
            }
        }

        public void SerializeItem(object? obj, MapItemInfo item, ref BitTarget header)
        {
            if (obj == null)
            {
                header.WriteBitOff();
                header.Apply();
            }

            else SerializePossibleNullableItem(obj, item, ref header);
        }

        public void SerializeExactNonNullItem(object obj, MapItemInfo item)
        {
            var currentHeader = new BitTarget(this);
            SerializeItemNoSetup(obj, item, ref currentHeader, true);
        }

        public void SerializeExactNonNullItem(object obj, MapItemInfo item, ref BitTarget header) =>
            SerializeItemNoSetup(obj, item, ref header, true);

        public void SerializePossibleNullableItem(object obj, MapItemInfo info, ref BitTarget header)
        {
            // Say it's "not null" if it is nullable.
            if (info.IsNullable) header.WriteBitOn();
            SerializeItemNoSetup(obj, info, ref header, info.IsNullable);
        }

        void SerializeItemNoSetup(object obj, MapItemInfo info, ref BitTarget header, bool skipHeader)
        {
            MapItem item = info.InnerItem;
            ABSaveUtils.WaitUntilNotGenerating(item);

            switch (item)
            {
                case Converter converter:
                    SerializeConverterItem(obj, converter, ref header, skipHeader);
                    break;
                case RuntimeMapItem runtime:
                    SerializeItemNoSetup(obj, new MapItemInfo(runtime.InnerItem, info.IsNullable), ref header, skipHeader);
                    break;
                default:
                    throw new Exception("ABSAVE: Unrecognized map item.");
            }
        }

        void SerializeConverterItem(object obj, Converter converter, ref BitTarget header, bool skipHeader)
        {
            Type? actualType = obj.GetType();

            bool appliedHeader = true;

            // Write the null and inheritance bits.
            bool sameType = true;
            if (!converter.IsValueItemType && !skipHeader)
            {
                sameType = WriteHeaderNullAndInheritance(actualType, converter, ref header);
                appliedHeader = false;
            }

            // Write and get the info for a version, if necessary
            if (!HandleVersionNumber(converter, out VersionInfo info, ref header))
                appliedHeader = true;

            // Handle inheritance if needed.
            if (info._inheritanceInfo != null && !sameType)
            {
                SerializeActualType(obj, info._inheritanceInfo, converter.ItemType, actualType, ref header);
                return;
            }

            // Apply the header if it's not being used and hasn't already been applied.
            if (!info.UsesHeader && !appliedHeader)
                header.Apply();

            var serializeInfo = new Converter.SerializeInfo(obj, actualType, info);
            converter.Serialize(in serializeInfo, ref header);
        }

        // Returns: Whether the type has changed.
        bool WriteHeaderNullAndInheritance(Type actualType, MapItem item, ref BitTarget target)
        {
            target.WriteBitOn(); // Null

            bool sameType = item.ItemType == actualType;
            target.WriteBitWith(sameType);
            return sameType;
        }

        /// <summary>
        /// Handles the version info for a given converter. If the version hasn't been written yet, it's written now. If not, nothing is written.
        /// </summary>
        /// <returns>Whether the item already exists</returns>
        internal bool HandleVersionNumber(Converter item, out VersionInfo info, ref BitTarget header)
        {
            // If the version has already been written, do nothing
            if (_versions.TryGetValue(item.ItemType, out VersionInfo? newInfo))
            {
                info = newInfo;
                return true;
            }

            uint version = WriteNewVersionInfo(item, ref header);

            info = Map.GetVersionInfo(item, version);
            _versions.Add(item.ItemType, info);
            return false;
        }

        uint WriteNewVersionInfo(Converter item, ref BitTarget target)
        {
            uint targetVersion = 0;

            // Try to get the custom target version and if there is none use the latest.
            if (TargetVersions?.TryGetValue(item.ItemType, out targetVersion) != true)
                targetVersion = item.HighestVersion;

            WriteCompressed(targetVersion, ref target);
            return targetVersion;
        }

        // Returns: Whether the sub-type was converted in here and we should return now.
        void SerializeActualType(object obj, SaveInheritanceAttribute info, Type baseType, Type actualType, ref BitTarget header)
        {
            switch (info.Mode)
            {
                case SaveInheritanceMode.Index:
                    if (!TryWriteListInheritance(info, actualType, ref header))
                        throw new UnsupportedSubTypeException(baseType, actualType);

                    break;
                case SaveInheritanceMode.Key:
                    WriteKeyInheritance(info, baseType, actualType, ref header);

                    break;
                case SaveInheritanceMode.IndexOrKey:
                    if (TryWriteListInheritance(info, actualType, ref header))
                        header.WriteBitOn();
                    else
                    {
                        header.WriteBitOff();
                        WriteKeyInheritance(info, baseType, actualType, ref header);
                    }

                    break;
            }

            // Serialize the actual type now.
            SerializeItemNoSetup(obj, GetRuntimeMapItem(actualType), ref header, true);
        }

        bool TryWriteListInheritance(SaveInheritanceAttribute info, Type actualType, ref BitTarget header)
        {
            if (info.IndexSerializeCache!.TryGetValue(actualType, out uint pos))
            {
                WriteCompressed(pos, ref header);
                return true;
            }

            return false;
        }

        void WriteKeyInheritance(SaveInheritanceAttribute info, Type baseType, Type actualType, ref BitTarget header)
        {
            string key = KeyInheritanceHandler.GetOrAddTypeKeyFromCache(baseType, actualType, info);
            WriteString(key, ref header);
        }

        void SerializeActualType(object obj, Type type)
        {
            MapItemInfo info = GetRuntimeMapItem(type);

            var newTarget = new BitTarget(this);
            SerializeItemNoSetup(obj, info, ref newTarget, true);
        }
    }
}
