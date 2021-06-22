using ABCo.ABSave.Mapping.Description;
using ABCo.ABSave.Mapping.Description.Attributes;
using ABCo.ABSave.Mapping.Generation.Object;
using System;
using System.Diagnostics;

namespace ABCo.ABSave.Mapping.Generation.IntermediateObject
{
    // This is created first before an object map is made. This gathers the raw data from either the
    // attributes or externally defined maps, and then translate it all, alongside a lot of analysis data, into an
    // intermediary form, that can then used to make the final maps for all the different version numbers.
    internal static class IntermediateMapper
    {
        public static uint CreateIntermediateObjectInfo(Type type, SaveMembersMode mode, out ObjectIntermediateItem[] members)
        {
            Debug.Assert(Attribute.IsDefined(type, typeof(SaveMembersAttribute)));

            var ctx = new IntermediateMappingContext(type, mode);

            // Coming soon: Settings-based mapping
            members = IntermediateReflectionMapper.FillInfo(ref ctx);

            if (ctx.TranslationCurrentOrderInfo == -1)
            {
                Array.Sort(members);
            }

            return ctx.HighestVersion;
        }

        internal static void FillMainInfo(ObjectIntermediateItem newItem, int order, int startVer, int endVer)
        {
            newItem.Order = order;

            // If the version given is -1, that means it doesn't have a set end, so we'll just fill that in with "uint.MaxValue".            
            newItem.StartVer = checked((uint)startVer);
            newItem.EndVer = endVer == -1 ? uint.MaxValue : checked((uint)endVer);
        }

        internal static void UpdateContextFromItem(ref IntermediateMappingContext ctx, ObjectIntermediateItem newItem)
        {
            // Check ordering
            if (ctx.TranslationCurrentOrderInfo != -1)
            {
                if (newItem.Order >= ctx.TranslationCurrentOrderInfo)
                {
                    ctx.TranslationCurrentOrderInfo = newItem.Order;
                }
                else
                {
                    ctx.TranslationCurrentOrderInfo = -1;
                }
            }

            MappingHelpers.UpdateHighestVersionFromRange(ref ctx.HighestVersion, newItem.StartVer, newItem.EndVer);
        }
    }
}
