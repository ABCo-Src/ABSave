using ABCo.ABSave.Mapping.Description.Attributes;
using System;
using System.Reflection;

namespace ABCo.ABSave.Mapping.Generation.Inheritance
{
    public static class InheritanceHandler
    {
        internal static SaveInheritanceAttribute[]? GetInheritanceAttributes(Type classType, ref uint highestVersion) =>
            // Coming soon: Setting-based mapping
            GetInheritanceAttributesByReflection(classType, ref highestVersion);

        internal static SaveInheritanceAttribute[]? GetInheritanceAttributesByReflection(Type classType, ref uint highestVersion)
        {
            var inheritanceInfo = (SaveInheritanceAttribute[])classType.GetCustomAttributes<SaveInheritanceAttribute>(false);
            if (inheritanceInfo == null) return null;

            MappingHelpers.ProcessVersionedAttributes(ref highestVersion, inheritanceInfo);
            return inheritanceInfo;
        }
    }
}
