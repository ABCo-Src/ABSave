using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization;
using ABSoftware.ABSave.Serialization.Writer;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave
{
    public static class ABSaveObjectConverter
    {
        public static Dictionary<Type, FieldInfo[]> CachedFieldInfos = new Dictionary<Type, FieldInfo[]>();

        public static void AutoSerializeObject(object obj, TypeInformation typeInformation, ABSaveWriter writer)
        {
            var info = GetObjectMemberInfos(typeInformation.ActualType);

            writer.WriteInt32((uint)info.Length);
            for (int i = 0; i < info.Length; i++)
            {
                if (writer.Settings.WithNames)
                    writer.WriteText(info[i].Name);

                var val = info[i].GetValue(obj);
                var actualType = val.GetType();
                var specifiedType = info[i].FieldType;

                ABSaveItemSerializer.SerializeAuto(val, new TypeInformation(actualType, Type.GetTypeCode(actualType), specifiedType, Type.GetTypeCode(specifiedType)), writer);
            }
        }

        public static FieldInfo[] GetObjectMemberInfos(Type typ)
        {
            if (CachedFieldInfos.TryGetValue(typ, out FieldInfo[] res)) return res;

            var info = typ.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            CachedFieldInfos.Add(typ, info);
            return info;
        }
    }
}
