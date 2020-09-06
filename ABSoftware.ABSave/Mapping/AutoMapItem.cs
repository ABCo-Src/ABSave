using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Mapping
{
    public class AutoMapItem : ABSaveMapItem
    {
        public AutoMapItem() : base(false) { }

        public override void Serialize(object obj, Type type, ABSaveWriter writer) => ABSaveItemConverter.SerializeWithAttribute(obj, type, writer);
        public override object Deserialize(Type type, ABSaveReader reader) => ABSaveItemConverter.DeserializeWithAttribute(type, reader);
    }
}
