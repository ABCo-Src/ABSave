using ABCo.ABSave.Configuration;
using ABCo.ABSave.Converters;
using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description;
using ABCo.ABSave.Mapping.Description.Attributes;
using ABCo.ABSave.Mapping.Generation.Inheritance;
using System;
using System.Collections.Generic;
using System.IO;

namespace ABCo.ABSave.Deserialization
{
    public sealed partial class ABSaveDeserializer : IDisposable
    {
        readonly Dictionary<Type, VersionInfo> _versions = new Dictionary<Type, VersionInfo>();

        public ABSaveMap Map { get; }
        public ABSaveSettings Settings { get; }
        public bool ShouldReverseEndian { get; }
        public Stream Source { get; private set; }

        byte[]? _stringBuffer;

        internal ABSaveDeserializer(ABSaveMap map)
        {
            Map = map;
            Settings = map.Settings;
            ShouldReverseEndian = map.Settings.UseLittleEndian != BitConverter.IsLittleEndian;
            Source = null!;
        }

        public void Initialize(Stream source)
        {
            Source = source;
            Reset();
        }

        public void Reset() => _versions.Clear();
        public void Dispose() => Map.ReleaseDeserializer(this);

        BitSource _currentHeader;
        bool _readHeader;

        public MapItemInfo GetRuntimeMapItem(Type type) => Map.GetRuntimeMapItem(type);

        public object? DeserializeRoot() => DeserializeItem(Map._rootItem);

        public object? DeserializeItem(MapItemInfo info)
        {
            // Do null checks
            if (info.IsValueTypeItem)
            {
                _currentHeader = new BitSource() { Deserializer = this };
                _readHeader = false;
            }
            else
            {
                _currentHeader = new BitSource(this);
                if (!_currentHeader.ReadBit()) return null;

                _readHeader = true;
            }

            return DeserializeNullableItem(info, false);
        }

        public object DeserializeExactNonNullItem(MapItemInfo info)
        {
            _currentHeader = new BitSource() { Deserializer = this };
            _readHeader = false;
            return DeserializeItemNoSetup(info, true);
        }

        public object? DeserializeItem(MapItemInfo info, ref BitSource header)
        {
            // Do null checks
            if (!info.IsValueTypeItem && !header.ReadBit()) return null;

            _currentHeader = header;
            _readHeader = true;
            return DeserializeNullableItem(info, false);
        }

        public object DeserializeExactNonNullItem(MapItemInfo info, ref BitSource header)
        {
            _currentHeader = header;
            _readHeader = true;
            return DeserializeItemNoSetup(info, true);
        }

        object? DeserializeNullableItem(MapItemInfo info, bool skipHeader)
        {
            if (info.IsNullable)
            {
                EnsureReadHeader();
                if (!_currentHeader.ReadBit()) return null;
                skipHeader = true;
            }

            return DeserializeItemNoSetup(info, skipHeader);
        }

        object DeserializeItemNoSetup(MapItemInfo info, bool skipHeader)
        {
            Converter item = info.Converter;
            ABSaveUtils.WaitUntilNotGenerating(item);

            return DeserializeConverter(info.Converter, skipHeader);
        }

        private object DeserializeConverter(Converter converter, bool skipHeader)
        {
            // Handle the inheritance bit.
            bool sameType = true;
            if (!skipHeader)
                sameType = ReadHeader(converter);

            // Read or create the version info if needed
            HandleVersionNumber(converter, out VersionInfo info, ref _currentHeader);

            // Handle inheritance.
            if (info._inheritanceInfo != null && !sameType)
                return DeserializeActualType(info._inheritanceInfo, converter.ItemType);

            var deserializeInfo = new Converter.DeserializeInfo(converter.ItemType, info);
            return converter.Deserialize(in deserializeInfo, ref _currentHeader);
        }

        /// <summary>
        /// Handles the version info for a given converter. If the version hasn't been read yet, it's read now. If not, nothing is read.
        /// </summary>
        /// <returns>Whether the item already exists</returns>
        internal void HandleVersionNumber(Converter item, out VersionInfo info, ref BitSource header)
        {
            // If the version has already been read, don't do anything.
            if (_versions.TryGetValue(item.ItemType, out VersionInfo? newInfo))
            {
                info = newInfo;
                return;
            }

            uint version = ReadNewVersionInfo(ref header);
            info = Map.GetVersionInfo(item, version);
            _versions.Add(item.ItemType, info);
        }
        
        // Returns: Whether the type has changed
        bool ReadHeader(Converter item)
        {
            if (item.IsValueItemType) return false;

            EnsureReadHeader();

            // Type
            return _currentHeader.ReadBit();
        }

        uint ReadNewVersionInfo(ref BitSource header) => ReadCompressedInt(ref header);

        // Returns: Whether the sub-type was converted in here and we should return now.
        object DeserializeActualType(SaveInheritanceAttribute info, Type baseType)
        {
            Type? actualType = info.Mode switch
            {
                SaveInheritanceMode.Index => TryReadListInheritance(info, baseType),
                SaveInheritanceMode.Key => TryReadKeyInheritance(info, baseType),
                SaveInheritanceMode.IndexOrKey => _currentHeader.ReadBit() ? TryReadListInheritance(info, baseType) : TryReadKeyInheritance(info, baseType),
                _ => throw new Exception("Invalid save inheritance mode")
            };

            if (actualType == null) throw new InvalidSubTypeInfoException(baseType);

            // Deserialize the actual type.
            return DeserializeItemNoSetup(GetRuntimeMapItem(actualType), true);
        }

        Type? TryReadListInheritance(SaveInheritanceAttribute info, Type baseType)
        {
            uint key = ReadCompressedInt(ref _currentHeader);
            return info.IndexDeserializeCache.GetValueOrDefault(key);
        }

        Type? TryReadKeyInheritance(SaveInheritanceAttribute info, Type baseType)
        {
            // Make sure the info is initialized for deserialization.
            KeyInheritanceHandler.EnsureHasAllTypeCache(baseType, info);

            // Read in the key from the source.
            string key = ReadString(ref _currentHeader);

            // See if there's an item with that key.
            return info.KeyDeserializeCache!.GetValueOrDefault(key);
        }

        void EnsureReadHeader()
        {
            if (!_readHeader)
            {
                _currentHeader = new BitSource(this, 8);
                _readHeader = true;
            }
        }
    }
}
