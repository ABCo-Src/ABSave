using ABSoftware.ABSave.Converters;
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
        internal bool CanBeNull = false;

        public ABSaveMapItem(bool canBeNull) => CanBeNull = canBeNull;

        public bool SerializeNullAttribute(object obj, ABSaveWriter writer)
        {
            if (CanBeNull)
            {
                if (obj == null)
                {
                    writer.WriteNullAttribute();
                    return true;
                }

                writer.WriteMatchingTypeAttribute();
            }

            return false;
        }

        public bool DeserializeNullAttribute(ABSaveReader reader)
        {
            if (CanBeNull) return reader.ReadByte() == 1;
            return false;
        }

        public abstract void Serialize(object obj, Type type, ABSaveWriter writer);
        public abstract object Deserialize(Type type, ABSaveReader reader);
    }
}
