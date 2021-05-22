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
        readonly Dictionary<MapItem, ObjectMemberSharedInfo[]> _typeVersions = new Dictionary<MapItem, ObjectMemberSharedInfo[]>();

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
            SerializeItemNoSetup(obj, item, ref currentHeader, true);
        }

        public void SerializeExactNonNullItem(object obj, MapItemInfo item, ref BitTarget target) => 
            SerializeItemNoSetup(obj, item, ref target, true);

        public void SerializePossibleNullableItem(object obj, MapItemInfo info, ref BitTarget target)
        {
            // Say it's "not null" if it is nullable.
            if (info.IsNullable) target.WriteBitOn();
            SerializeItemNoSetup(obj, info, ref target, info.IsNullable);
        }

        void SerializeItemNoSetup(object obj, MapItemInfo info, ref BitTarget target, bool skipHeader)
        {
            MapItem item = info._innerItem;
            ABSaveUtils.WaitUntilNotGenerating(item);

            switch (item)
            {
                case ConverterContext ctx:
                    SerializeConverterItem(obj, ctx, ref target, skipHeader);
                    break;
                case ObjectMapItem objMap:
                    SerializeObjectItem(obj, objMap, ref target, skipHeader);
                    break;
                case RuntimeMapItem runtime:
                    SerializeItemNoSetup(obj, new MapItemInfo(runtime.InnerItem, info.IsNullable), ref target, skipHeader);
                    break;
                default:
                    throw new Exception("ABSAVE: Unrecognized map item.");
            }
        }

        void SerializeConverterItem(object obj, ConverterContext ctx, ref BitTarget target, bool skipHeader)
        {
            var actualType = obj.GetType();

            if (!skipHeader)
            {
                bool convSubTypes = ctx.Converter.ConvertsSubTypes;
                bool writesToHeader = ctx.Converter.WritesToHeader;

                if (WriteHeader(convSubTypes, writesToHeader, actualType, ctx, ref target))
                {
                    SerializeActualType(obj, actualType);
                    return;
                }
            }

            ctx.Converter.Serialize(obj, actualType, ctx, ref target);
        }


        void SerializeObjectItem(object obj, ObjectMapItem item, ref BitTarget target, bool skipHeader)
        {
            var actualType = obj.GetType();

            // Write the header
            if (!skipHeader)
            {
                if (WriteHeader(false, true, actualType, item, ref target))
                {
                    SerializeActualType(obj, actualType);
                    return;
                }
            }

            // Write the members
            if (_typeVersions.TryGetValue(item, out ObjectMemberSharedInfo[]? val))
                SerializeFromMembers(obj, val);
            else
                SerializeObjectNewVersion(obj, item, ref target);
        }

        private void SerializeObjectNewVersion(object obj, ObjectMapItem item, ref BitTarget target)
        {
            int targetVersion = 0;

            // Try to get the custom target version and if there is none use the latest.
            if (TargetVersions?.TryGetValue(item.ItemType, out targetVersion) != true)
                targetVersion = item.ObjectHighestVersion;

            // Write the verion number.
            WriteCompressed((uint)targetVersion, ref target);

            // Get and write the members.
            ObjectMemberSharedInfo[] info = Map.GetMembersForVersion(item, targetVersion);
            SerializeFromMembers(obj, info);
            _typeVersions.Add(item, info);
        }

        void SerializeFromMembers(object obj, ObjectMemberSharedInfo[] members)
        {
            for (int i = 0; i < members.Length; i++)
                SerializeItem(members[i].Accessor.Getter(obj), members[i].Map);
        }

        // Returns: True if the type differed, and needs to be serialized seperately, false if not.
        static bool WriteHeader(bool mapConvSubTypes, bool mapHasHeader, Type actualType, MapItem item, ref BitTarget target)
        {
            if (item.IsValueItemType) return false;

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
            SerializeItemNoSetup(obj, info, ref newTarget, true);
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
