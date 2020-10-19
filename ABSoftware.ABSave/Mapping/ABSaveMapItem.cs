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

        public void Serialize(object obj, Type specifiedType, ABSaveWriter writer)
        {
            if (CanBeNull)
            {
                if (obj == null)
                {
                    writer.WriteNullAttribute();
                    return;
                }
                else writer.WriteMatchingTypeAttribute();
            }

            DoSerialize(obj, specifiedType, writer);
        }

        public object Deserialize(Type specifiedType, ABSaveReader reader)
        {
            if (CanBeNull && reader.ReadByte() == 1) return null;
            return DoDeserialize(specifiedType, reader);
        }

        protected abstract void DoSerialize(object obj, Type specifiedType, ABSaveWriter writer);
        protected abstract object DoDeserialize(Type specifiedType, ABSaveReader reader);        
    }
}
