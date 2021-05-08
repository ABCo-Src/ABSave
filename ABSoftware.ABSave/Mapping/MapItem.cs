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
        public MapItemType MapType;
        public bool IsValueType;
        public volatile bool IsGenerating;
        public ExtraData Extra;
        public MainData Main;

        // Extra data stored in the free 4 bytes.
        [StructLayout(LayoutKind.Explicit)]
        public struct ExtraData
        {
            [FieldOffset(0)]
            public int ObjectHighestVersion;

            [FieldOffset(0)]
            public MapItemInfo RuntimeInnerItem;
        }

        // Main data made up of 2 references.
        [StructLayout(LayoutKind.Explicit)]
        public struct MainData
        {
            [FieldOffset(0)]
            public ObjectMapItem Object;

            [FieldOffset(0)]
            public ConverterMapItem Converter;
        }
    }

    public enum MapItemType : byte
    {
        Converter,
        Object,
        Runtime
    }

    internal struct ObjectMapItem
    {
        public Dictionary<int, ObjectMemberInfo[]?> Versions; 

        // Null once all versions have been generated.
        public IntermediateObjInfo? IntermediateInfo;
    }

    internal struct ConverterMapItem
    {
        public Converter Converter;
        public IConverterContext Context;
    }

    internal struct ObjectMemberInfo
    {
        public MapItemInfo Map;
        public MemberAccessor Accessor;
    }
}