using ABSoftware.ABSave.Exceptions;
using ABSoftware.ABSave.Mapping;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave
{
    public static class ABSaveObjectConverter
    {
        public static void Serialize(object obj, Type type, ABSaveWriter writer)
        {
            var info = GetObjectMemberInfos(type, writer.Settings);

            writer.WriteInt32((uint)info.Length);
            for (int i = 0; i < info.Length; i++)
            {
                writer.WriteString(info[i].Name);
                ABSaveItemConverter.SerializeWithAttribute(info[i].GetValue(obj), info[i].FieldType, writer);
            }
        }

        internal static void Serialize(object obj, Type type, ABSaveWriter writer, ObjectMapItem item)
        {
            writer.WriteInt32((uint)item.NumberOfItems);
            
            for (int i = 0; i < item.NumberOfItems; i++)
            {
                writer.WriteString(item.Items[i].Name);

                if (item.Items[i].UseReflection)
                {
                    var field = type.GetField(item.Items[i].Name, writer.Settings.MemberReflectionFlags);
                    item.Items[i].Serialize(field.GetValue(obj), field.FieldType, writer);
                }
                else item.Items[i].Serialize(item.Items[i].Getter(obj), item.Items[i].FieldType, writer);
            }
        }

        public static object Deserialize(Type type, ABSaveReader reader)
        {
            var obj = Activator.CreateInstance(type);

            var numberOfItems = reader.ReadInt32();
            for (int i = 0; i < numberOfItems; i++)
            {
                var field = type.GetField(reader.ReadString(), reader.Settings.MemberReflectionFlags);

                if (field == null)
                {
                    if (reader.Settings.ErrorOnUnknownItem) throw new ABSaveObjectUnmatchingException();
                }
                else field.SetValue(obj, ABSaveItemConverter.DeserializeWithAttribute(field.FieldType, reader));
            }

            return obj;
        }

        public static object Deserialize(Type type, ABSaveReader reader, ObjectMapItem item)
        {
            var obj = Activator.CreateInstance(type);

            var numberOfItems = reader.ReadInt32();
            for (int i = 0; i < numberOfItems; i++)
            {
                var itemName = reader.ReadString();

                if (item.HashedItems.TryGetValue(itemName, out ABSaveMapItem mapItem))
                    if (mapItem.UseReflection)
                    {
                        var field = type.GetField(mapItem.Name, reader.Settings.MemberReflectionFlags);
                        field.SetValue(obj, mapItem.Deserialize(field.FieldType, reader));
                    }
                    else mapItem.Setter(obj, mapItem.Deserialize(mapItem.FieldType, reader));

                else if (reader.Settings.ErrorOnUnknownItem) throw new ABSaveObjectUnmatchingException();
            }

            return obj;
        }

        public static FieldInfo[] GetObjectMemberInfos(Type typ, ABSaveSettings settings) => typ.GetFields(settings.MemberReflectionFlags);
    }
}
