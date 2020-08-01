using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization.Writer;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave.Mapping
{
    public abstract class ABSaveMapItem
    {
        internal string Name;
        internal bool UseReflection = true;
        internal Func<object, object> Getter = null;
        internal Action<object, object> Setter = null;
        internal Type FieldType = null;

        public void SerializeAndGenerateActualType(object obj, TypeInformation typeInfo, ABSaveWriter writer)
        {

        }

        public abstract void Serialize(object obj, TypeInformation typeInfo, ABSaveWriter writer);
    }
}
