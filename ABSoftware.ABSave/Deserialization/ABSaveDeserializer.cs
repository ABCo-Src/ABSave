using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Exceptions;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ABSoftware.ABSave.Deserialization
{
    public sealed partial class ABSaveDeserializer
    {
        readonly Dictionary<MapItem, ObjectMemberSharedInfo[]> _typeVersions = new Dictionary<MapItem, ObjectMemberSharedInfo[]>();

        internal List<Assembly> SavedAssemblies = new List<Assembly>();
        internal List<Type> SavedTypes = new List<Type>();

        public ABSaveMap Map { get; private set; } = null!;
        public ABSaveSettings Settings { get; private set; } = null!;
        public Stream Source { get; private set; } = null!;
        public bool ShouldReverseEndian { get; private set; }

        byte[]? _stringBuffer;

        public void Initialize(Stream source, ABSaveMap map)
        {
            Map = map;
            Settings = map.Settings;
            ShouldReverseEndian = Map.Settings.UseLittleEndian != BitConverter.IsLittleEndian;
            Source = source;
        }

        public void Reset()
        {
            SavedAssemblies.Clear();
            SavedTypes.Clear();
            _typeVersions.Clear();
        }

        BitSource _currentHeader;
        bool _readHeader;
    
        public MapItemInfo GetRuntimeMapItem(Type type) => Map.GetRuntimeMapItem(type);

        public object? DeserializeRoot()
        {
            return DeserializeItem(Map.RootItem);
        }

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
            MapItem item = info._innerItem;
            ABSaveUtils.WaitUntilNotGenerating(item);

            return item switch
            {
                ConverterContext converter => DeserializeConverterItem(converter, skipHeader),
                ObjectMapItem objItem => DeserializeObjectItem(objItem, skipHeader),
                RuntimeMapItem runtime => DeserializeItemNoSetup(new MapItemInfo(runtime.InnerItem, info.IsNullable), skipHeader),
                _ => throw new Exception("Unrecognized map item"),
            };
        }

        private object DeserializeConverterItem(ConverterContext context, bool skipHeader)
        {
            if (!skipHeader)
            {
                bool convertsSubTypes = context.Converter.ConvertsSubTypes;
                bool writesToHeader = context.Converter.WritesToHeader;

                Type? actualType = ReadHeader(convertsSubTypes, writesToHeader, context);
                if (actualType != null) return DeserializeItemNoSetup(GetRuntimeMapItem(actualType), true);
            }
            
            return context.Converter.Deserialize(context.ItemType, context, ref _currentHeader);
        }

        private object DeserializeObjectItem(ObjectMapItem item, bool skipHeader)
        {
            if (!skipHeader)
            {
                Type? actualType = ReadHeader(false, true, item);
                if (actualType != null) return DeserializeItemNoSetup(GetRuntimeMapItem(actualType), true);
            }

            return DeserializeObjectMembers(item.ItemType, item);
        }

        object DeserializeObjectMembers(Type type, ObjectMapItem item)
        {
            var res = Activator.CreateInstance(type);
            
            if (_typeVersions.TryGetValue(item, out ObjectMemberSharedInfo[]? val))
                DeserializeFromMembers(val!);
            else
            {
                // Deserialize the version in the file.
                uint version = ReadCompressedInt(ref _currentHeader);

                ObjectMemberSharedInfo[] info = Map.GetMembersForVersion(item, version);
                _typeVersions.Add(item, info);
                DeserializeFromMembers(info);
            }

            return res!;

            void DeserializeFromMembers(ObjectMemberSharedInfo[] members)
            {
                for (int i = 0; i < members.Length; i++)
                    members[i].Accessor.Setter(res!, DeserializeItem(members[i].Map));
            }
        }

        // Returns: The actual type, null if it's the same as the specified type.
        Type? ReadHeader(bool mapSupportsSub, bool mapUsesHeader, MapItem item)
        {
            if (item.IsValueItemType)
            {
                if (mapUsesHeader) EnsureReadHeader();
                return null;
            }

            // Type checks
            if (!mapSupportsSub)
            {
                EnsureReadHeader();

                // Matching type
                if (_currentHeader.ReadBit()) return null;

                var actualType = ReadClosedType(item.ItemType, ref _currentHeader);

                // The header was used by the type
                _readHeader = false;
                return actualType;
            }

            return null;
        }

        void EnsureReadHeader()
        {
            if (!_readHeader)
            {
                _currentHeader = new BitSource(this, 8);
                _readHeader = true;
            }
        }

        // TODO: Use map guides to implement proper "Type" handling via map.
        public Type ReadType(Type requiredBaseType)
        {
            var header = new BitSource(this);
            return ReadType(requiredBaseType, ref header);
        }

        public Type ReadType(Type requiredBaseType, ref BitSource header)
        {
            var res = TypeConverter.Instance.DeserializeType(ref header);

            // Safety check.
            if (!res.IsSubclassOf(requiredBaseType)) throw new UnexpectedTypeException(requiredBaseType, res);

            return res;
        }

        public Type ReadClosedType(Type requiredBaseType) 
        {
            var header = new BitSource(this);
            return ReadClosedType(requiredBaseType, ref header);
        }

        public Type ReadClosedType(Type requiredBaseType, ref BitSource header)
        {
            var res = TypeConverter.Instance.DeserializeClosedType(ref header);

            // Safety check.
            if (!res.IsSubclassOf(requiredBaseType)) throw new UnexpectedTypeException(requiredBaseType, res);

            return res;
        }
    }
}
