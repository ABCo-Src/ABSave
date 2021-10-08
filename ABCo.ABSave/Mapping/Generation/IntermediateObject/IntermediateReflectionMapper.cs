using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Mapping.Description;
using ABCo.ABSave.Mapping.Description.Attributes;
using ABCo.ABSave.Mapping.Generation.IntermediateObject;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ABCo.ABSave.Mapping.Generation.Object
{
    /// <summary>
    /// A class responsible for creating intermediate info about an object via reflection.
    /// </summary>
    internal static class IntermediateReflectionMapper
    {
        public static IntermediateItem[] FillInfo(ref IntermediateMappingContext ctx)
        {
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            Type? classType = ctx.ClassType;

            // Get the members
            FieldInfo[] currentFields = GetFields(bindingFlags, classType, ctx.Mode);
            PropertyInfo[] currentProperties = GetProperties(bindingFlags, classType, ctx.Mode);

            // Process the members
            var dest = new List<IntermediateItem>(currentFields.Length + currentProperties.Length);
            AddMembers(ref ctx, currentFields, dest);
            AddMembers(ref ctx, currentProperties, dest);
            return dest.ToArray();
        }

        static void AddMembers(ref IntermediateMappingContext ctx, MemberInfo[] members, List<IntermediateItem> dest)
        {
            for (int i = 0; i < members.Length; i++)
            {
                IntermediateItem? newItem = GetItemForMember(ref ctx, members[i]);
                if (newItem != null) dest.Add(newItem);
            }
        }

        internal static IntermediateItem? GetItemForMember(ref IntermediateMappingContext ctx, MemberInfo info)
        {
            // Get the attributes - skip this item if there are none
            object[]? attributes = info.GetCustomAttributes(typeof(SaveAttribute), false);
            if (attributes.Length == 0) return null;

            // Create the item.
            var newItem = CreateItemFromAttributes(info, (SaveAttribute[])attributes);
            if (newItem == null) throw new IncompleteDetailsException(info);

            IntermediateMapper.UpdateContextFromItem(ref ctx, newItem);
            return newItem;
        }

        static IntermediateItem? CreateItemFromAttributes(MemberInfo info, SaveAttribute[] attributes)
        {
            IntermediateItem dest;

            if (attributes.Length == 1)
            {
                dest = new IntermediateItem();
                IntermediateMapper.FillMainInfo(dest, attributes[0].Order, attributes[0].FromVer, attributes[0].ToVer);
            }
            else
            {
                dest = new IntermediateItemWithMultipleOrders(attributes);

                MappingHelpers.ProcessVersionedAttributes(ref dest.EndVer, attributes);
                dest.StartVer = attributes[0].FromVer;
            }

            dest.Details.Unprocessed = info;
            return dest;
        }

        static FieldInfo[] GetFields(BindingFlags bindingFlags, Type classType, SaveMembersMode mode)
        {
            FieldInfo[]? fields = Array.Empty<FieldInfo>();

            if ((mode & SaveMembersMode.Fields) > 0)
                fields = classType.GetFields(bindingFlags);

            return fields;
        }

        static PropertyInfo[] GetProperties(BindingFlags bindingFlags, Type classType, SaveMembersMode mode)
        {
            PropertyInfo[]? properties = Array.Empty<PropertyInfo>();

            if ((mode & SaveMembersMode.Properties) > 0)
                properties = classType.GetProperties(bindingFlags);

            return properties;
        }
    }
}
