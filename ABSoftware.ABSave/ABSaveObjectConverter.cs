using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave
{
    public static class ABSaveObjectConverter
    {
        public static Dictionary<Type, MemberInfo[]> CachedMemberInfos = new Dictionary<Type, MemberInfo[]>();

        public static void AutoSerializeObject(object obj, ABSaveWriter writer, TypeInformation typeInformation)
        {
            var info = GetObjectMemberInfos(typeInformation.ActualType);

            writer.WriteInt32((uint)info.Length);
            for (int i = 0; i < info.Length; i++)
            {
                if (writer.Settings.WithNames)
                    writer.WriteText(info[i].Name);

                object val;
                Type specifiedType;

                if (info[i] is FieldInfo field)
                {
                    val = field.GetValue(obj);
                    specifiedType = field.FieldType;
                }

                else if (info[i] is PropertyInfo property)
                {
                    val = property.GetValue(obj);
                    specifiedType = property.PropertyType;
                }

                else throw new Exception("ABSave: Not field or property info");

                var actualType = val.GetType();

                ABSaveSerializer.SerializeAuto(val, writer, new TypeInformation(actualType, Type.GetTypeCode(actualType), specifiedType, Type.GetTypeCode(specifiedType)));
            }
        }

        internal static MemberInfo[] GetMembers(Type typ)
        {
            var fields = typ.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var properties = typ.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var ret = new MemberInfo[fields.Length + properties.Length];

            Array.Copy(fields, ret, fields.Length);
            Array.Copy(properties, 0, ret, fields.Length, properties.Length);

            return ret;
        }

        public static MemberInfo[] GetObjectMemberInfos(Type typ)
        {
            var info = CachedMemberInfos[typ];
            if (info != null) return info;

            info = GetMembers(typ);
            CachedMemberInfos.Add(typ, info);
            return info;
        }
    }
}
