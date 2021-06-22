﻿using ABCo.ABSave.Exceptions;
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
        public static ObjectIntermediateItem[] FillInfo(ref IntermediateMappingContext ctx)
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var classType = ctx.ClassType;

            // Get the members
            FieldInfo[] currentFields = GetFields(bindingFlags, classType, ctx.Mode);
            PropertyInfo[] currentProperties = GetProperties(bindingFlags, classType, ctx.Mode);

            // Process the members
            var dest = new List<ObjectIntermediateItem>(currentFields.Length + currentProperties.Length);
            AddMembers(ref ctx, currentFields, dest);
            AddMembers(ref ctx, currentProperties, dest);
            return dest.ToArray();
        }

        static void AddMembers(ref IntermediateMappingContext ctx, MemberInfo[] members, List<ObjectIntermediateItem> dest)
        {
            for (int i = 0; i < members.Length; i++)
            {
                var newItem = GetItemForMember(ref ctx, members[i]);
                if (newItem != null) dest.Add(newItem);
            }
        }

        internal static ObjectIntermediateItem? GetItemForMember(ref IntermediateMappingContext ctx, MemberInfo info)
        {
            // Get the attributes - skip this item if there are none
            var attributes = info.GetCustomAttributes(typeof(MapAttr), false);
            if (attributes.Length == 0) return null;

            var newItem = new ObjectIntermediateItem();

            // Create the item.
            bool successful = FillItemFromAttributes(newItem, info, attributes);
            if (!successful) throw new IncompleteDetailsException(info);

            IntermediateMapper.UpdateContextFromItem(ref ctx, newItem);
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
