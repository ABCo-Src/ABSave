﻿using ABSoftware.ABSave;
using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping;
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

namespace ABSoftware.ABSave.Serialization
{
    /// <summary>
    /// The central object that everything in ABSave writes to. Provides facilties to write primitive types, including strings.
    /// </summary>
    public sealed partial class ABSaveSerializer
    {
        internal Dictionary<Assembly, int> SavedAssemblies = new Dictionary<Assembly, int>();
        internal Dictionary<Type, int> SavedTypes = new Dictionary<Type, int>();

        public ABSaveMap Map { get; private set; }
        public ABSaveSettings Settings { get; private set; }
        public Stream Output { get; private set; }
        public bool ShouldReverseEndian { get; private set; }

        byte[] _stringBuffer;

        public void Initialize(Stream output, ABSaveMap map)
        {
            if (!output.CanWrite)
                throw new Exception("Cannot use unwriteable stream.");

            Output = output;

            Map = map;
            Settings = map.Settings;

            ShouldReverseEndian = map.Settings.UseLittleEndian != BitConverter.IsLittleEndian;

            Reset();
        }

        public void Reset()
        {
            SavedAssemblies.Clear();
            SavedTypes.Clear();
        }

        public MapItemInfo GetRuntimeMapItem(Type type) => ABSaveUtils.GetRuntimeMapItem(type, Map);

        public void SerializeRoot(object obj)
        {
            SerializeItem(obj, Map.RootItem);
        }

        public void SerializeItem(object obj, MapItemInfo item)
        {
            if (obj == null)
                WriteByte(0);

            else
            {
                var currentHeader = new BitTarget(this);
                SerializeItemNoSetup(obj, obj.GetType(), item, ref currentHeader, false);
            }
        }

        public void SerializeItem(object obj, MapItemInfo item, ref BitTarget target)
        {
            if (obj == null)
            {
                target.WriteBitOff();
                target.Apply();
            }
            else SerializeItemNoSetup(obj, obj.GetType(), item, ref target, false);
        }

        public void SerializeExactNonNullItem(object obj, MapItemInfo item)
        {
            var currentHeader = new BitTarget(this);
            SerializeItemNoSetup(obj, obj.GetType(), item, ref currentHeader, true);
        }

        public void SerializeExactNonNullItem(object obj, MapItemInfo item, ref BitTarget target) => SerializeItemNoSetup(obj, obj.GetType(), item, ref target, true);

        void SerializeItemNoSetup(object obj, Type actualType, MapItemInfo info, ref BitTarget target, bool skipAllHeaderHandling)
        {
            ref MapItem item = ref Map.GetItemAt(info);
            ABSaveUtils.WaitUntilNotGenerating(ref item);

            // Null items are already handled in the setup, however it is our responsibility to say that the item "is not null" if necessary.
            if (info.IsNullable)
            {
                target.WriteBitOn(); // Clearly wasn't null
                skipAllHeaderHandling = true;
            }

            switch (item.MapType)
            {
                case MapItemType.Converter:

                    ref ConverterMapItem converter = ref MapItem.GetConverterData(ref item);

                    if (WriteHeader(converter.Converter.ConvertsSubTypes, converter.Converter.WritesToHeader, ref item, ref target)) return;
                    converter.Converter.Serialize(obj, actualType, converter.Context, ref target);

                    break;

                case MapItemType.Object:

                    ref ObjectMapItem objItem = ref MapItem.GetObjectData(ref item);

                    if (WriteHeader(false, false, ref item, ref target)) return;
                    SerializeObjectItems(obj, ref objItem);

                    break;

                case MapItemType.Runtime:

                    MapItemInfo inner = MapItem.GetRuntimeExtraData(ref item);

                    // Switch to the item inside.
                    SerializeItemNoSetup(obj, actualType, inner, ref target, skipAllHeaderHandling);
                    break;

                default:
                    throw new Exception("ABSAVE: Unrecognized map item.");
            }

            // Parameters: The type should not be taken into consideration when choosing parameters, only what the map items themselves support.
            // Returns: True if the type differed, and has been serialized seperately, false if not.
            bool WriteHeader(bool convertsSubTypes, bool usesHeader, ref MapItem item, ref BitTarget target)
            {
                if (skipAllHeaderHandling || item.IsValueType) return false;

                // Null
                target.WriteBitOn();

                // Type checks
                if (Settings.SaveInheritance && !convertsSubTypes)
                {
                    if (item.ItemType == actualType)
                        target.WriteBitOn();

                    else
                    {
                        target.WriteBitOff();
                        WriteClosedType(actualType, ref target);

                        // Serialize again, but with the new map. The header got used up by the type so we need to use a new one.
                        var newTarget = new BitTarget(this);
                        SerializeItemNoSetup(obj, actualType, GetRuntimeMapItem(actualType), ref newTarget, true);
                        return true;
                    }
                        
                }

                // Make sure to apply everything we write if necessary
                if (!usesHeader) target.Apply();

                return false;
            }
        }

        void SerializeObjectItems(object obj, ref ObjectMapItem map)
        {
            // Serialize the item
            for (int i = 0; i < map.Members.Length; i++)
                SerializeItem(GetValue(ref map.Members[i]), map.Members[i].Map);

            object GetValue(ref ObjectMemberInfo member)
                => member.Accessor.Getter(obj);
        }

        // TODO: Use map guides to implement proper "Type" handling via map.
        public void WriteType(Type type)
        {
            var header = new BitTarget(this);
            WriteType(type, ref header);
        }

        public void WriteType(Type type, ref BitTarget header) => TypeConverter.Instance.SerializeType(type, ref header);

        public void WriteClosedType(Type type)
        {
            var header = new BitTarget(this);
            WriteClosedType(type, ref header);
        }

        public void WriteClosedType(Type type, ref BitTarget header) => TypeConverter.Instance.SerializeClosedType(type, ref header);
    }
}
