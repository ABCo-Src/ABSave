using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Exceptions;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Mapping.Items;
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

        public ABSaveMap Map { get; }
        public ABSaveSettings Settings { get; }
        public Stream Source { get; }
        public bool ShouldReverseEndian { get; }

        byte[] _stringBuffer;

        public ABSaveDeserializer(Stream source, ABSaveMap map)
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

        #region Type Conversion + Attributes

        BitSource _currentHeader;
        bool _readHeader;
    
        public MapItem GetRuntimeMapItem(Type type) => ABSaveUtils.GetRuntimeMapItem(type, Map);

        public object DeserializeRoot()
        {
            return DeserializeItem(Map.RootItem);
        }

        public object DeserializeItem(MapItem map)
        {
            // Do null checks
            if (map.ItemType.IsValueType)
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
            
            return DeserializeItemNoSetup(map, false);
        }

        public object DeserializeExactNonNullItem(MapItem map)
        {
            _currentHeader = new BitSource() { Deserializer = this };
            _readHeader = false;
            return DeserializeItemNoSetup(map, true);
        }

        public object DeserializeItem(MapItem map, ref BitSource header)
        {
            // Do null checks
            if (!map.ItemType.IsValueType && !header.ReadBit()) return null;
            
            _currentHeader = header;
            _readHeader = true;
            return DeserializeItemNoSetup(map, false);
        }

        public object DeserializeExactNonNullItem(MapItem map, ref BitSource header)
        {
            _currentHeader = header;
            _readHeader = true;
            return DeserializeItemNoSetup(map, true);
        }

        object DeserializeItemNoSetup(MapItem map, bool skipHeaderHandling)
        {
            // Null has already been handled.
            switch (map)
            {
                case ConverterMapItem converterItem:

                    object actual = ReadHeader(converterItem.Converter.ConvertsSubTypes, converterItem.Converter.WritesToHeader);
                    if (actual != null) return actual;

                    return converterItem.Converter.Deserialize(map.ItemType.Type, converterItem.Context, ref _currentHeader);

                case ObjectMapItem objectItem:
                    object actualObj = ReadHeader(false, false);
                    if (actualObj != null) return actualObj;

                    return DeserializeObjectItems(objectItem);

                case NullableMapItem nullableItem:

                    EnsureReadHeader();
                    if (!_currentHeader.ReadBit()) return null;
                    return DeserializeItemNoSetup(nullableItem.InnerItem, true);

                case RuntimeMapItem runtime:
                    return DeserializeItemNoSetup(runtime.InnerItem, skipHeaderHandling);

                default:
                    throw new Exception("Unrecognized map item");
            }

            object ReadHeader(bool mapSupportsSub, bool mapUsesHeader)
            {
                if (skipHeaderHandling || map.ItemType.IsValueType)
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

                    var actualType = ReadClosedType(map.ItemType.Type, ref _currentHeader);

                    // The header was used by the type
                    _readHeader = false;
                    return DeserializeItemNoSetup(GetRuntimeMapItem(actualType), true);
                }

                return null;
            }
        }

        object DeserializeObjectItems(ObjectMapItem map)
        {
            var res = Activator.CreateInstance(map.ItemType.Type);

            for (int i = 0; i < map.Members.Length; i++)
                SetValue(DeserializeItem(map.Members[i].Map), ref map.Members[i]);

            return res;

            void SetValue(object val, ref ObjectMemberInfo member)
            {
                // Field
                if (member.Info.Left != null) member.Info.Left.SetValue(res, val);

                // Property
                else member.Info.Right.Setter(res, val);
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

        #endregion

        // TODO: Use map guides to implement proper "Type" handling via map.
        public Type ReadType(Type requiredBaseType)
        {
            var header = new BitSource(this);
            return ReadType(requiredBaseType, ref header);
        }

        public Type ReadType(Type requiredBaseType, ref BitSource header)
        {
            var res = TypeConverter.Instance.DeserializeType(Map.AssemblyItem, ref header);

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
            var res = TypeConverter.Instance.DeserializeClosedType(Map.AssemblyItem, ref header);

            // Safety check.
            if (!res.IsSubclassOf(requiredBaseType)) throw new ABSaveUnexpectedTypeException(requiredBaseType, res);

            return res;
        }
    }
}
