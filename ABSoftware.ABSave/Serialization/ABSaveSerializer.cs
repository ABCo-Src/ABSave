using ABCo.ABSave;
using ABCo.ABSave.Configuration;
using ABCo.ABSave.Converters;
using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description;
using ABCo.ABSave.Mapping.Description.Attributes;
using ABCo.ABSave.Mapping.Generation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Serialization;

namespace ABCo.ABSave.Serialization
{
    /// <summary>
    /// The central object that everything in ABSave writes to. Provides facilties to write primitive types, including strings.
    /// </summary>
    public sealed partial class ABSaveSerializer
    {
        readonly Dictionary<Type, ObjectVersionInfo> _objectVersions = new Dictionary<Type, ObjectVersionInfo>();
        readonly Dictionary<Type, ConverterVersionInfo> _converterVersions = new Dictionary<Type, ConverterVersionInfo>();

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

        public void Reset()
        {
            _objectVersions.Clear();
            _converterVersions.Clear();
        }

        public MapItemInfo GetRuntimeMapItem(Type type) => Map.GetRuntimeMapItem(type);

        public void SerializeRoot(object? obj)
        {
            SerializeItem(obj, Map.RootItem);
        }

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
            MapItem item = info._innerItem;
            ABSaveUtils.WaitUntilNotGenerating(item);

            switch (item)
            {
                case Converter converter:
                    SerializeConverterItem(obj, converter, ref header, skipHeader);
                    break;
                case ObjectMapItem objMap:
                    SerializeObjectItem(obj, objMap, ref header, skipHeader);
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
            var actualType = obj.GetType();

            bool appliedHeader = true;

            // Write the null and inheritance bits.
            bool sameType = true;
            if (!converter.IsValueItemType && !skipHeader)
            {
                sameType = WriteHeaderNullAndInheritance(actualType, converter, ref header);
                appliedHeader = false;
            }

            // Write and get the info for a version, if necessary
            if (!_converterVersions.TryGetValue(converter.ItemType, out ConverterVersionInfo? info))
            {
                uint version = WriteNewVersionInfo(converter, ref header);
                appliedHeader = true;

                // Get the converter version info
                bool usesHeader;
                (info, usesHeader) = converter.GetVersionInfo(version);
                
                // TODO: Pool this allocation.
                info ??= new ConverterVersionInfo(usesHeader);
                info.Initialize(version, usesHeader, converter);

                _converterVersions.Add(converter.ItemType, info);
            }

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

        void SerializeObjectItem(object obj, ObjectMapItem item, ref BitTarget header, bool skipHeader)
        {
            var actualType = obj.GetType();

            // Write the null and inheritance bits.
            bool sameType = true;
            if (!item.IsValueItemType && !skipHeader) 
                sameType = WriteHeaderNullAndInheritance(actualType, item, ref header);

            // Write and get the info for a version, if necessary
            if (!_objectVersions.TryGetValue(item.ItemType, out ObjectVersionInfo info))
            {
                uint version = WriteNewVersionInfo(item, ref header);
                info = MapGenerator.GetVersionOrAddNull(version, item);
                _objectVersions.Add(item.ItemType, info);
            }

            // Handle inheritance if needed.
            if (info.InheritanceInfo != null && !sameType)
            {
                SerializeActualType(obj, info.InheritanceInfo, item.ItemType, actualType, ref header);
                return;
            }

            SerializeFromMembers(obj, info.Members!);
        }

        void SerializeFromMembers(object obj, ObjectMemberSharedInfo[] members)
        {
            for (int i = 0; i < members.Length; i++)
                SerializeItem(members[i].Accessor.Getter(obj), members[i].Map);
        }

        // Returns: Whether the type has changed.
        bool WriteHeaderNullAndInheritance(Type actualType, MapItem item, ref BitTarget target)
        {
            target.WriteBitOn(); // Null

            bool sameType = item.ItemType == actualType;
            target.WriteBitWith(sameType);
            return sameType;
        }

        uint WriteNewVersionInfo(MapItem item, ref BitTarget target)
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
            var info = GetRuntimeMapItem(type);

            var newTarget = new BitTarget(this);
            SerializeItemNoSetup(obj, info, ref newTarget, true);
        }
    }
}
