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
        public Type ItemType { get; set; }
        public bool IsValueType { get; set; }
    }
}
