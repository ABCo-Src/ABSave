using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Mapping.Description;
using ABCo.ABSave.Mapping.Description.Attributes;
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
        public ObjectIntermediateItem[] FillInfo(out SaveInheritanceAttribute[]? inheritanceInfo)
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var classType = Gen._intermediateContext.ClassType;
            var mode = GetClassInfo(classType, out inheritanceInfo);

            // Get the members
            FieldInfo[] _currentFields = GetFields(bindingFlags, classType, mode);
            PropertyInfo[] _currentProperties = GetProperties(bindingFlags, classType, mode);

            PrepareBuffer(_currentFields.Length + _currentProperties.Length);

            // Process the members
            return ProcessMembers();
        }

        static void ProcessMembers(MemberInfo[] members, List<ObjectIntermediateItem> dest)
        {
            for (int i = 0; i < members.Length; i++)
                dest.Add(GetItemForMember(members[i]));
        }

        internal static ObjectIntermediateItem? GetItemForMember(MemberInfo info)
        {
            // Get the attributes - skip this item if there are none
            var attributes = info.GetCustomAttributes(typeof(MapAttr), false);
            if (attributes.Length == 0) return null;

            var newItem = new ObjectIntermediateItem();

            // Create the item.
            bool successful = FillItemFromAttributes(newItem, info, attributes);
            if (!successful) throw new IncompleteDetailsException(info);

            dest = newItem;
            count++;
        }

        private static bool FillItemFromAttributes(ObjectIntermediateItem dest, MemberInfo info, object[] attributes)
        {
            newItem.Details.Unprocessed = info;

            bool loadedSaveAttribute = false;
            for (int i = 0; i < attributes.Length; i++)
            {
                switch (attributes[i])
                {
                    case SaveAttribute save:
                        FillMainInfo(newItem, save.Order, save.FromVer, save.ToVer);
                        loadedSaveAttribute = true;
                        break;
                }
            }

            return loadedSaveAttribute;
        }

        SaveMembersMode GetClassInfo(Type classType, out SaveInheritanceAttribute[]? inheritanceInfo)
        {
            // TODO: This is just to temporarily support "object" until proper settings mapping comes in.
            SaveMembersMode res = GetClassMode(classType);

            inheritanceInfo = (SaveInheritanceAttribute[])classType.GetCustomAttributes<SaveInheritanceAttribute>(false);

            if (inheritanceInfo.Length == 0)
                inheritanceInfo = null;
            else
            {
                for (int i = 0; i < inheritanceInfo.Length; i++)
                {
                    var info = inheritanceInfo[i];
                    Gen.UpdateVersionInfo(info.FromVer, info.ToVer);
                }
            }

            return res;
        }

        private static SaveMembersMode GetClassMode(Type classType)
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
