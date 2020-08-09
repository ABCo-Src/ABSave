using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization;
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

        public abstract void Serialize(object obj, Type type, ABSaveWriter writer);
        public abstract object Deserialize(Type type, ABSaveReader reader);
    }
}
