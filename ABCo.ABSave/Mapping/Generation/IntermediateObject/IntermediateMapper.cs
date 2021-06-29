using ABCo.ABSave.Mapping.Description;
using ABCo.ABSave.Mapping.Description.Attributes;
using ABCo.ABSave.Mapping.Generation.Object;
using System;
using System.Diagnostics;
using System.Reflection;

namespace ABCo.ABSave.Mapping.Generation.IntermediateObject
{
    // This is created first before an object map is made. This gathers the raw data from either the
    // attributes or externally defined maps, and then translate it all, alongside a lot of analysis data, into an
    // intermediary form, that can then used to make the final maps for all the different version numbers.
    internal static class IntermediateMapper
    {
        public static uint CreateIntermediateObjectInfo(Type type, SaveMembersMode mode, out IntermediateObjectInfo intermediateInfo)
        {
            Debug.Assert(Attribute.IsDefined(type, typeof(SaveMembersAttribute)));

            var ctx = new IntermediateMappingContext(type, mode);

            // Coming soon: Settings-based mapping
            intermediateInfo = new IntermediateObjectInfo
            {
                Members = IntermediateReflectionMapper.FillInfo(ref ctx),
                BaseMemberAttributes = GetBaseMembersAttributes(ref ctx, type)
            };

            if (ctx.TranslationCurrentOrderInfo == -1)
                Array.Sort(intermediateInfo.Members);

            return ctx.HighestVersion;
        }

        internal static void FillMainInfo(IntermediateItem newItem, int order, uint startVer, uint endVer)
        {
            newItem.Order = order;
            newItem.StartVer = startVer;
            newItem.EndVer = endVer;
        }

        internal static void UpdateContextFromItem(ref IntermediateMappingContext ctx, IntermediateItem newItem)
        {
            // Check ordering
            if (ctx.TranslationCurrentOrderInfo != -1)
            {
                if (newItem.Order >= ctx.TranslationCurrentOrderInfo)
                    ctx.TranslationCurrentOrderInfo = newItem.Order;
                else
                    ctx.TranslationCurrentOrderInfo = -1;
            }

            MappingHelpers.UpdateHighestVersionFromRange(ref ctx.HighestVersion, newItem.StartVer, newItem.EndVer);
        }

        internal static SaveBaseMembersAttribute[]? GetBaseMembersAttributes(ref IntermediateMappingContext ctx, Type type)
        {
            SaveBaseMembersAttribute[] attr = (SaveBaseMembersAttribute[])type.GetCustomAttributes<SaveBaseMembersAttribute>(false);
            if (attr.Length == 0) return null;

            MappingHelpers.ProcessVersionedAttributes(ref ctx.HighestVersion, attr);
            return attr;
        }
    }
}