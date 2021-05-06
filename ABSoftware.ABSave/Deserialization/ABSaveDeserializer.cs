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
        readonly Dictionary<MapItemInfo, ObjectMemberInfo[]> _typeVersions = new Dictionary<MapItemInfo, ObjectMemberInfo[]>();

        internal List<Assembly> SavedAssemblies = new List<Assembly>();
        internal List<Type> SavedTypes = new List<Type>();

        public ABSaveMap Map { get; private set; }
        public ABSaveSettings Settings { get; private set; }
        public Stream Source { get; private set; }
        public bool ShouldReverseEndian { get; private set; }

        byte[] _stringBuffer;

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
    
        public MapItemInfo GetRuntimeMapItem(Type type) => ABSaveUtils.GetRuntimeMapItem(type, Map);

        public object DeserializeRoot()
        {
            return DeserializeItem(Map.RootItem);
        }

        public object DeserializeItem(MapItemInfo info)
        {
            ref MapItem item = ref Map.GetItemAt(info);

            // Do null checks
            if (item.IsValueType)
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
            
            return DeserializeItemNoSetup(info, ref Map.GetItemAt(info), info.Pos.Flag, false);
        }

        public object DeserializeExactNonNullItem(MapItemInfo info)
        {
            _currentHeader = new BitSource() { Deserializer = this };
            _readHeader = false;
            return DeserializeItemNoSetup(info, ref Map.GetItemAt(info), info.Pos.Flag, true);
        }

        public object DeserializeItem(MapItemInfo info, ref BitSource header)
        {
            ref MapItem item = ref Map.GetItemAt(info);

            // Do null checks
            if (!item.IsValueType && !header.ReadBit()) return null;
            
            _currentHeader = header;
            _readHeader = true;
            return DeserializeItemNoSetup(info, ref item, info.Pos.Flag, false);
        }

        public object DeserializeExactNonNullItem(MapItemInfo info, ref BitSource header)
        {
            _currentHeader = header;
            _readHeader = true;
            return DeserializeItemNoSetup(info, ref Map.GetItemAt(info), info.Pos.Flag, true);
        }

        object DeserializeItemNoSetup(MapItemInfo itemPos, ref MapItem item, bool isNullable, bool skipHeaderHandling)
        {
            // Null has already been handled in the setup.
            if (isNullable)
            {
                EnsureReadHeader();
                if (!_currentHeader.ReadBit()) return null;
                skipHeaderHandling = true;
            }

            switch (item.MapType)
            {
                // Converter
                case MapItemType.Converter:

                    ref ConverterMapItem converter = ref item.Main.Converter;

                    object actual = ReadHeader(converter.Converter.ConvertsSubTypes, converter.Converter.WritesToHeader, ref item);
                    if (actual != null) return actual;

                    return converter.Converter.Deserialize(item.ItemType, converter.Context, ref _currentHeader);

                // Object
                case MapItemType.Object:

                    object actualObj = ReadHeader(false, true, ref item);
                    if (actualObj != null) return actualObj;

                    return DeserializeObjectItems(item.ItemType, itemPos, ref item);

                // Runtime
                case MapItemType.Runtime:

                    MapItemInfo inner = item.Extra.RuntimeInnerItem;
                    return DeserializeItemNoSetup(inner, ref Map.GetItemAt(inner), inner.Pos.Flag, skipHeaderHandling);

                default:
                    throw new Exception("Unrecognized map item");
            }

            object ReadHeader(bool mapSupportsSub, bool mapUsesHeader, ref MapItem item)
            {
                if (skipHeaderHandling || item.IsValueType)
                {
                    if (mapUsesHeader) EnsureReadHeader();
                    return null;
                }

                // Type checks
                if (Settings.SaveInheritance && !mapSupportsSub)
                {
                    EnsureReadHeader();

                    // Matching type
                    if (_currentHeader.ReadBit()) return null;

                    var actualType = ReadClosedType(item.ItemType, ref _currentHeader);

                    // The header was used by the type
                    _readHeader = false;

                    var info = GetRuntimeMapItem(actualType);
                    return DeserializeItemNoSetup(info, ref Map.GetItemAt(info), info.Pos.Flag, true);
                }

                return null;
            }
        }

        object DeserializeObjectItems(Type type, MapItemInfo itemPos, ref MapItem item)
        {
            var res = Activator.CreateInstance(type);

            if (_typeVersions.TryGetValue(itemPos, out ObjectMemberInfo[] val))
                DeserializeFromMembers(val);
            else
            {
                // Deserialize the version in the file.
                int version = (int)ReadCompressedInt(ref _currentHeader);

                ObjectMemberInfo[] info = Map.GetMembersForVersion(ref item, version);
                _typeVersions.Add(itemPos, info);
                DeserializeFromMembers(info);
            }

            return res;

            void DeserializeFromMembers(ObjectMemberInfo[] members)
            {
                for (int i = 0; i < members.Length; i++)
                    members[i].Accessor.Setter(res, DeserializeItem(members[i].Map));
            }
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
            if (!res.IsSubclassOf(requiredBaseType)) throw new ABSaveUnexpectedTypeException(requiredBaseType, res);

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
            if (!res.IsSubclassOf(requiredBaseType)) throw new ABSaveUnexpectedTypeException(requiredBaseType, res);

            return res;
        }
    }
}
