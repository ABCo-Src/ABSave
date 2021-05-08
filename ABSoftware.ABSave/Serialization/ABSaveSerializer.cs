using ABSoftware.ABSave;
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
        readonly Dictionary<MapItemInfo, ObjectMemberInfo[]> _typeVersions = new Dictionary<MapItemInfo, ObjectMemberInfo[]>();

        internal Dictionary<Assembly, int> SavedAssemblies = new Dictionary<Assembly, int>();
        internal Dictionary<Type, int> SavedTypes = new Dictionary<Type, int>();

        public Dictionary<Type, int>? TargetVersions { get; private set; }
        public ABSaveMap Map { get; private set; } = null!;
        public ABSaveSettings Settings { get; private set; } = null!;
        public Stream Output { get; private set; } = null!;
        public bool ShouldReverseEndian { get; private set; }

        byte[]? _stringBuffer;

        public void Initialize(Stream output, ABSaveMap map, Dictionary<Type, int>? targetVersions)
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
            SavedAssemblies.Clear();
            SavedTypes.Clear();
            _typeVersions.Clear();
        }

        public MapItemInfo GetRuntimeMapItem(Type type) => ABSaveUtils.GetRuntimeMapItem(type, Map);

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

        public void SerializeItem(object? obj, MapItemInfo item, ref BitTarget target)
        {
            if (obj == null)
            {
                target.WriteBitOff();
                target.Apply();
            }

            else SerializePossibleNullableItem(obj, item, ref target);
        }

        public void SerializeExactNonNullItem(object obj, MapItemInfo item)
        {
            var currentHeader = new BitTarget(this);
            SerializeItemNoSetup(obj, obj.GetType(), item, ref currentHeader, true);
        }

        public void SerializeExactNonNullItem(object obj, MapItemInfo item, ref BitTarget target) => 
            SerializeItemNoSetup(obj, obj.GetType(), item, ref target, true);

        public void SerializePossibleNullableItem(object obj, MapItemInfo info, ref BitTarget target)
        {
            // Say it's "not null" if it is nullable.
            if (info.IsNullable) target.WriteBitOn();
            SerializeItemNoSetup(obj, obj.GetType(), info, ref target, info.IsNullable);
        }

        void SerializeItemNoSetup(object obj, Type actualType, MapItemInfo info, ref BitTarget target, bool skipHeader)
        {
            ref MapItem item = ref Map.GetItemAt(info);
            ABSaveUtils.WaitUntilNotGenerating(ref item);

            switch (item.MapType)
            {
                case MapItemType.Converter:
                    SerializeConverterItem(obj, actualType, ref item, ref target, skipHeader);
                    break;
                case MapItemType.Object:
                    SerializeObjectItem(obj, actualType, info, ref item, ref target, skipHeader);
                    break;
                case MapItemType.Runtime:
                    SerializeItemNoSetup(obj, actualType, item.Extra.RuntimeInnerItem, ref target, skipHeader);
                    break;
                default:
                    throw new Exception("ABSAVE: Unrecognized map item.");
            }
        }

        void SerializeConverterItem(object obj, Type actualType, ref MapItem item, ref BitTarget target, bool skipHeader)
        {
            ref ConverterMapItem converter = ref item.Main.Converter;

            if (WriteHeader(converter.Converter.ConvertsSubTypes, converter.Converter.WritesToHeader, actualType, ref item, ref target, skipHeader))
            {
                SerializeActualType(obj, actualType);
                return;
            }

            converter.Converter.Serialize(obj, actualType, converter.Context, ref target);
        }


        void SerializeObjectItem(object obj, Type actualType, MapItemInfo mapPos, ref MapItem item, ref BitTarget target, bool skipHeader)
        {
            if (WriteHeader(false, true, actualType, ref item, ref target, skipHeader))
            {
                SerializeActualType(obj, actualType);
                return;
            }

            if (_typeVersions.TryGetValue(mapPos, out ObjectMemberInfo[]? val))
                SerializeFromMembers(val);
            else
            {
                int targetVersion = 0;

                // Try to get the custom target version and if there is none use the latest.
                if (TargetVersions?.TryGetValue(item.ItemType, out targetVersion) != true)
                    targetVersion = item.Extra.ObjectHighestVersion;

                WriteCompressed((uint)targetVersion, ref target);

                ObjectMemberInfo[] info = Map.GetMembersForVersion(ref item, targetVersion);
                SerializeFromMembers(info);
                _typeVersions.Add(mapPos, info);
            }

            void SerializeFromMembers(ObjectMemberInfo[] members)
            {
                for (int i = 0; i < members.Length; i++)
                    SerializeItem(members[i].Accessor.Getter(obj), members[i].Map);
            }
        }

        // Returns: True if the type differed, and needs to be serialized seperately, false if not.
        static bool WriteHeader(bool mapConvSubTypes, bool mapHasHeader, Type actualType, ref MapItem item, ref BitTarget target, bool skipHeader)
        {
            if (skipHeader || item.IsValueType) return false;

            // Null
            target.WriteBitOn();

            // Type checks
            if (!mapConvSubTypes)
            {
                if (item.ItemType == actualType)
                    target.WriteBitOn();

                else
                {
                    target.WriteBitOff();
                    WriteClosedType(actualType, ref target);
                    return true;
                }
            }

            // Make sure to apply everything we write if necessary
            if (!mapHasHeader) target.Apply();

            return false;
        }

        void SerializeActualType(object obj, Type type)
        {
            var info = GetRuntimeMapItem(type);

            var newTarget = new BitTarget(this);
            SerializeItemNoSetup(obj, type, info, ref newTarget, true);
        }

        // TODO: Use map guides to implement proper "Type" handling via map.
        public void WriteType(Type type)
        {
            var header = new BitTarget(this);
            WriteType(type, ref header);
        }

        public static void WriteType(Type type, ref BitTarget header) => TypeConverter.Instance.SerializeType(type, ref header);

        public void WriteClosedType(Type type)
        {
            var header = new BitTarget(this);
            WriteClosedType(type, ref header);
        }

        public static void WriteClosedType(Type type, ref BitTarget header) => TypeConverter.Instance.SerializeClosedType(type, ref header);
    }
}
