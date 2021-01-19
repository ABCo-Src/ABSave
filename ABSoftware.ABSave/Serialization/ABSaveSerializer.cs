using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Mapping.Items;
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

        public ABSaveMap Map { get; }
        public ABSaveSettings Settings { get; }
        public Stream Output { get; }
        public bool ShouldReverseEndian { get; }

        byte[] _stringBuffer;

        public ABSaveSerializer(Stream output, ABSaveMap map) 
        {
            if (!output.CanWrite)
                throw new Exception("Cannot use unwriteable stream.");

            Output = output;

            Map = map;
            Settings = map.Settings;
            
            ShouldReverseEndian = map.Settings.UseLittleEndian != BitConverter.IsLittleEndian;
        }

        public void Reset()
        {
            SavedAssemblies.Clear();
            SavedTypes.Clear();
        }

        public MapItem GetRuntimeMapItem(Type type) => ABSaveUtils.GetRuntimeMapItem(type, Map);

        public void SerializeRoot(object obj)
        {
            SerializeItem(obj, Map.RootItem);
        }

        public void SerializeItem(object obj, MapItem item)
        {
            if (obj == null)
                WriteByte(0);

            else
            {
                var currentHeader = new BitTarget(this);
                if (!item.ItemType.IsValueType) currentHeader.WriteBitOn();
                SerializeItemNoSetup(obj, obj.GetType(), item, ref currentHeader, item.ItemType.IsValueType);
            }
        }

        public void SerializeItem(object obj, MapItem item, ref BitTarget target)
        {
            if (obj == null)
            {
                target.WriteBitOff();
                target.Apply();
            }
            else
            {
                if (!item.ItemType.IsValueType) target.WriteBitOn();
                SerializeItemNoSetup(obj, obj.GetType(), item, ref target, item.ItemType.IsValueType);
            }
        }

        public void SerializeExactNonNullItem(object obj, MapItem item)
        {
            var currentHeader = new BitTarget(this);
            SerializeItemNoSetup(obj, obj.GetType(), item, ref currentHeader, true);
        }

        public void SerializeExactNonNullItem(object obj, MapItem item, ref BitTarget target) => SerializeItemNoSetup(obj, item.ItemType.Type, item, ref target, true);        

        void SerializeItemNoSetup(object obj, Type actualType, MapItem item, ref BitTarget target, bool skipTypeHandling)
        {
            switch (item)
            {
                case ConverterMapItem converterItem:

                    SerializeConverterMap(obj, actualType, converterItem, ref target, skipTypeHandling);
                    break;

                case ObjectMapItem objItem:

                    SerializeObjectMap(obj, actualType, objItem, ref target, skipTypeHandling);
                    break;

                case NullableMapItem nullable:

                    target.WriteBitOn(); // This clearly wasn't null.
                    SerializeItemNoSetup(obj, actualType, nullable.InnerItem, ref target, true);
                    break;

                case RuntimeMapItem runtime:
                    SerializeItemNoSetup(obj, actualType, runtime.InnerItem, ref target, skipTypeHandling);
                    break;

                default:
                    throw new Exception("ABSAVE: Unrecognized map item.");
            }
        }

        void SerializeConverterMap(object obj, Type actualType, ConverterMapItem map, ref BitTarget target, bool skipTypeHandling)
        {
            if (!skipTypeHandling)
            {
                if (!map.Converter.ConvertsSubTypes)
                {
                    // Matching type
                    if (map.ItemType.Type == actualType)
                        target.WriteBitOn();

                    // Different type
                    else
                    {
                        target.WriteBitOff();
                        WriteClosedType(actualType, ref target);

                        // Serialize again, but with the new map. The header got used up by the type so we need to use a new one.
                        var newTarget = new BitTarget(this);
                        SerializeItemNoSetup(obj, actualType, GetRuntimeMapItem(actualType), ref newTarget, true);
                        return;
                    }
                }

                // Since we've just put data into the header, we must make sure that gets applied properly.
                if (!map.Converter.WritesToHeader) target.Apply();
            }

            // Serialize the item
            map.Converter.Serialize(obj, actualType, map.Context, ref target);
        }

        void SerializeObjectMap(object obj, Type actualType, ObjectMapItem map, ref BitTarget target, bool skipTypeHandling)
        {
            if (!skipTypeHandling)
            {
                // Matching type
                if (map.ItemType.Type == actualType)
                {
                    target.WriteBitOn();
                    target.Apply();
                }

                // Different type
                else
                {
                    target.WriteBitOff();
                    WriteClosedType(actualType, ref target);

                    // Serialize again, but with the new map. The header got used up by the type so we need to use a new one.
                    var newTarget = new BitTarget(this);
                    SerializeItemNoSetup(obj, actualType, GetRuntimeMapItem(actualType), ref newTarget, true);
                    return;
                }
            }

            // Serialize the item
            for (int i = 0; i < map.Members.Length; i++)
                SerializeItem(GetValue(ref map.Members[i]), map.Members[i].Map);

            object GetValue(ref ObjectMemberInfo member)
            {
                // Field
                if (member.Info.Left != null) return member.Info.Left.GetValue(obj);

                // Property - reference types are cached at run-time to avoid reflection.
                else return member.Info.Right.Getter(obj);
            }
        }

        // TODO: Use map guides to implement proper "Type" handling via map.
        public void WriteType(Type type)
        {
            var header = new BitTarget(this);
            WriteType(type, ref header);
        }

        public void WriteType(Type type, ref BitTarget header) => TypeConverter.Instance.SerializeType(type, Map.AssemblyItem, ref header);

        public void WriteClosedType(Type type)
        {
            var header = new BitTarget(this);
            WriteClosedType(type, ref header);
        }

        public void WriteClosedType(Type type, ref BitTarget header) => TypeConverter.Instance.SerializeClosedType(type, Map.AssemblyItem, ref header);
    }
}
