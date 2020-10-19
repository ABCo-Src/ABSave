using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Mapping
{
    public class AutoMapItem : ABSaveMapItem
    {
        public AutoMapItem() : base(false) { }

        protected override void DoSerialize(object obj, Type specifiedType, ABSaveWriter writer) => ABSaveItemConverter.Serialize(obj, specifiedType, writer);
        protected override object DoDeserialize(Type specifiedType, ABSaveReader reader) => ABSaveItemConverter.DeserializeWithAttribute(specifiedType, reader);
    }
}
