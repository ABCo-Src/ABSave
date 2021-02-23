using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping.Generation;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ABSoftware.ABSave.Mapping
{
    [StructLayout(LayoutKind.Auto)]
    internal struct MapItem
    {
        public Type ItemType;
        public bool IsValueType;
        public bool IsGenerating;
        public MapItemType MapType;
        ExtraData _extra;
        MainData _data;

        // Extra data stored in the free 4 bytes of padding.
        [StructLayout(LayoutKind.Explicit)]
        struct ExtraData
        {
            //[FieldOffset(0)]
            //public int ObjectLatestVersion;

            [FieldOffset(0)]
            public MapItemInfo InnerItem;
        }

        // Main data stored as 2 references.
        [StructLayout(LayoutKind.Explicit)]
        struct MainData
        {
            [FieldOffset(0)]
            public ObjectMapItem Object;

            [FieldOffset(0)]
            public ConverterMapItem Converter;
        }

        public static ref ObjectMapItem GetObjectData(ref MapItem item) => ref item._data.Object;
        public static ref ConverterMapItem GetConverterData(ref MapItem item) => ref item._data.Converter;

        public static ref MapItemInfo GetRuntimeExtraData(ref MapItem item) => ref item._extra.InnerItem;
    }

    public enum MapItemType : byte
    {
        Object,
        Converter,
        Runtime
    }

    internal struct ObjectMapItem
    {
        public ObjectMemberInfo[] Members;
    }

    internal struct ObjectMemberInfo
    {
        public MapItemInfo Map;
        public MemberAccessor Accessor;
    }

    // Since it's relatively rare that we'll actually have a property with both a reference type item and
    // a reference types, we'll take the allocation hit at generation-time.
    internal class ObjectRefPropertyAccessor
    {
        internal Action<object, object> FastGetter;
        internal Func<object, object, object> FastSetter;
    }

    internal struct ConverterMapItem
    {
        public Converter Converter;
        public IConverterContext Context;
    }
}