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
        public static (IntermediateItem[] members, bool hasMemberWithMultipleOrders) FillInfo(ref IntermediateMappingContext ctx)
        {
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            Type? classType = ctx.ClassType;

            // Get the members
            FieldInfo[] currentFields = GetFields(bindingFlags, classType, ctx.Mode);
            PropertyInfo[] currentProperties = GetProperties(bindingFlags, classType, ctx.Mode);

            // Process the member.
            var dest = new List<IntermediateItem>(currentFields.Length + currentProperties.Length);

            bool hasMemberWithMultipleOrders = AddMembers(ref ctx, currentFields, dest);
            if (AddMembers(ref ctx, currentProperties, dest)) hasMemberWithMultipleOrders = true;

            return (dest.ToArray(), hasMemberWithMultipleOrders);
        }

        static bool AddMembers(ref IntermediateMappingContext ctx, MemberInfo[] members, List<IntermediateItem> dest)
        {
            bool hasMemberWithMultipleOrders = false;

            for (int i = 0; i < members.Length; i++)
            {
                IntermediateItem? newItem = GetItemForMember(ref ctx, members[i]);
                if (newItem != null)
                {
                    if (newItem.HasMultipleOrders) hasMemberWithMultipleOrders = true;
                    dest.Add(newItem);
                }
            }

            return hasMemberWithMultipleOrders;
        }

        internal static IntermediateItem? GetItemForMember(ref IntermediateMappingContext ctx, MemberInfo info)
        {
            // Get the attributes - skip this item if there are none
            object[]? attributes = info.GetCustomAttributes(typeof(SaveAttribute), false);
            if (attributes.Length == 0) return null;

            // Create the item.
            var newItem = CreateItemFromAttributes(ref ctx, info, (SaveAttribute[])attributes);
            if (newItem == null) throw new IncompleteDetailsException(info);

            return newItem;
        }

        static IntermediateItem? CreateItemFromAttributes(ref IntermediateMappingContext ctx, MemberInfo info, SaveAttribute[] attributes)
        {
            IntermediateItem dest;

            if (attributes.Length == 1)
            {
                dest = new IntermediateItem();
                IntermediateMapper.FillMainInfo(dest, attributes[0].Order, attributes[0].FromVer, attributes[0].ToVer);
                IntermediateMapper.UpdateContextFromSingleOrderItem(ref ctx, dest);
            }
            else
            {
                dest = new IntermediateItemWithMultipleOrders(attributes);

                bool containedUnsetItems = MappingHelpers.SortVersionedAttributes(out uint highestVersion, attributes);
                dest.FromVer = attributes[attributes.Length - 1].FromVer;
                dest.ToVer = containedUnsetItems ? uint.MaxValue : highestVersion;

                MappingHelpers.UpdateHighestVersionFromRange(ref ctx.HighestVersion, dest.FromVer, highestVersion);
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
