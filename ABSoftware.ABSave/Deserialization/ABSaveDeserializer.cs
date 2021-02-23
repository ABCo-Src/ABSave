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
            
            return DeserializeItemNoSetup(ref Map.GetItemAt(info), info.Pos.Flag, false);
        }

        public object DeserializeExactNonNullItem(MapItemInfo info)
        {
            _currentHeader = new BitSource() { Deserializer = this };
            _readHeader = false;
            return DeserializeItemNoSetup(ref Map.GetItemAt(info), info.Pos.Flag, true);
        }

        public object DeserializeItem(MapItemInfo info, ref BitSource header)
        {
            ref MapItem item = ref Map.GetItemAt(info);

            // Do null checks
            if (!item.IsValueType && !header.ReadBit()) return null;
            
            _currentHeader = header;
            _readHeader = true;
            return DeserializeItemNoSetup(ref item, info.Pos.Flag, false);
        }

        public object DeserializeExactNonNullItem(MapItemInfo info, ref BitSource header)
        {
            _currentHeader = header;
            _readHeader = true;
            return DeserializeItemNoSetup(ref Map.GetItemAt(info), info.Pos.Flag, true);
        }

        object DeserializeItemNoSetup(ref MapItem item, bool isNullable, bool skipHeaderHandling)
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
                case MapItemType.Converter:

                    ref ConverterMapItem converter = ref MapItem.GetConverterData(ref item);

                    object actual = ReadHeader(converter.Converter.ConvertsSubTypes, converter.Converter.WritesToHeader, ref item);
                    if (actual != null) return actual;

                    return converter.Converter.Deserialize(item.ItemType, converter.Context, ref _currentHeader);

                case MapItemType.Object:

                    ref ObjectMapItem objItem = ref MapItem.GetObjectData(ref item);

                    object actualObj = ReadHeader(false, false, ref item);
                    if (actualObj != null) return actualObj;

                    return DeserializeObjectItems(item.ItemType, ref objItem);

                case MapItemType.Runtime:
                    ref MapItemInfo nullableInner = ref MapItem.GetRuntimeExtraData(ref item);
                    return DeserializeItemNoSetup(ref Map.GetItemAt(nullableInner), nullableInner.Pos.Flag, skipHeaderHandling);

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
                    return DeserializeItemNoSetup(ref Map.GetItemAt(info), info.Pos.Flag, true);
                }

                return null;
            }
        }

        object DeserializeObjectItems(Type type, ref ObjectMapItem item)
        {
            var res = Activator.CreateInstance(type);

            for (int i = 0; i < item.Members.Length; i++)
                SetValue(DeserializeItem(item.Members[i].Map), ref item.Members[i]);

            return res;

            void SetValue(object val, ref ObjectMemberInfo member)
            {
                member.Accessor.Setter(res, val);
                // Field
                //if (member.Info.Left != null) member.Info.Left.SetValue(res, val);

                //// Property
                //else member.Info.Right.Setter(res, val);
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
