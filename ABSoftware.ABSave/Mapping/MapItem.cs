using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave.Mapping
{
    public abstract class MapItem
    {
        public MapItemType ItemType { get; set; }
    }

    public struct MapItemType
    {
        public Type Type { get; set; }
        public bool IsValueType { get; set; }

        public MapItemType(Type type, bool isValueType) => (Type, IsValueType) = (type, isValueType);
    }
}
