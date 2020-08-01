using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization;
using ABSoftware.ABSave.Serialization.Writer;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Mapping
{
    public class AutoMapItem : ABSaveMapItem
    {
        public override void Serialize(object obj, TypeInformation typeInfo, ABSaveWriter writer) => ABSaveItemSerializer.SerializeAuto(obj, typeInfo, writer);
    }
}
