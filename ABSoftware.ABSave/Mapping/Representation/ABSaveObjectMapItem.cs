using ABSoftware.ABSave.Converters;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave.Mapping.Representation
{
    public class ABSaveObjectMapItem : ABSaveMapItem
    {
        public ObjectFieldInfo[] Fields;

        public override void SerializeData(object obj, Type type, ABSaveWriter writer)
        {
            for (int i = 0; i < Fields.Length; i++)
            {
                var fieldInfo = Fields[i].Info;

                writer.WriteString(fieldInfo.Name);
                Fields[i].Map.SerializeData(fieldInfo.GetValue(obj), type, writer);
            }
        }
    }
}
