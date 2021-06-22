using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Mapping.Description;
using ABCo.ABSave.Mapping.Description.Attributes;
using ABCo.ABSave.Mapping.Generation.IntermediateObject;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ABCo.ABSave.Mapping.Generation.Object
{
    /// <summary>
    /// A class responsible for creating intermediate info about an object via reflection.
    /// </summary>
    internal static class IntermediateReflectionMapper
    {
        public static ObjectIntermediateItem[] FillInfo(ref IntermediateMappingContext ctx, out SaveInheritanceAttribute[]? inheritanceInfo)
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var classType = ctx.ClassType;
            var mode = GetClassInfo(ref ctx, classType, out inheritanceInfo);

            // Get the members
            FieldInfo[] currentFields = GetFields(bindingFlags, classType, mode);
            PropertyInfo[] currentProperties = GetProperties(bindingFlags, classType, mode);

            // Process the members
            var dest = new List<ObjectIntermediateItem>(currentFields.Length + currentProperties.Length);
            AddMembers(currentFields, dest);
            AddMembers(currentProperties, dest);
            return dest.ToArray();
        }

        static void AddMembers(MemberInfo[] members, List<ObjectIntermediateItem> dest)
        {
            for (int i = 0; i < members.Length; i++)
            {
                var newItem = GetItemForMember(members[i]);
                if (newItem != null) dest.Add(newItem);
            }
        }

        static ObjectIntermediateItem? GetItemForMember(MemberInfo info)
        {
            // Get the attributes - skip this item if there are none
            var attributes = info.GetCustomAttributes(typeof(MapAttr), false);
            if (attributes.Length == 0) return null;

            var newItem = new ObjectIntermediateItem();

            // Create the item.
            bool successful = FillItemFromAttributes(newItem, info, attributes);
            if (!successful) throw new IncompleteDetailsException(info);

            return newItem;
        }

        static bool FillItemFromAttributes(ObjectIntermediateItem dest, MemberInfo info, object[] attributes)
        {
            dest.Details.Unprocessed = info;

            bool loadedSaveAttribute = false;
            for (int i = 0; i < attributes.Length; i++)
            {
                switch (attributes[i])
                {
                    case SaveAttribute save:
                        IntermediateMapper.FillMainInfo(dest, save.Order, save.FromVer, save.ToVer);
                        loadedSaveAttribute = true;
                        break;
                }
            }

            return loadedSaveAttribute;
        }

        static SaveMembersMode GetClassInfo(ref IntermediateMappingContext ctx, Type classType, out SaveInheritanceAttribute[]? inheritanceInfo)
        {
            // TODO: This is just to temporarily support "object" until proper settings mapping comes in.
            SaveMembersMode res = GetClassMode(classType);


            return res;
        }

        static SaveMembersMode GetClassMode(Type classType)
        {
            if (classType == typeof(object)) return SaveMembersMode.Fields;

            var attribute = classType.GetCustomAttribute<SaveMembersAttribute>(false);
            if (attribute == null) throw new UnserializableType(classType);

            return attribute.Mode;
        }

        static FieldInfo[] GetFields(BindingFlags bindingFlags, Type classType, SaveMembersMode mode)
        {
            var fields = Array.Empty<FieldInfo>();

            if ((mode & SaveMembersMode.Fields) > 0)
                fields = classType.GetFields(bindingFlags);

            return fields;
        }

        static PropertyInfo[] GetProperties(BindingFlags bindingFlags, Type classType, SaveMembersMode mode)
        {
            var properties = Array.Empty<PropertyInfo>();

            if ((mode & SaveMembersMode.Properties) > 0)
                properties = classType.GetProperties(bindingFlags);

            return properties;
        }
    }
}
