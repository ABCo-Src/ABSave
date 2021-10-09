using ABCo.ABSave.Mapping.Description.Attributes;
using System;
using System.Reflection;

namespace ABCo.ABSave.Mapping.Generation.Inheritance
{
    public static class InheritanceHandler
    {
        internal static SaveInheritanceAttribute[]? GetInheritanceAttributes(Type classType, out uint highestVersion) =>
            // Coming soon: Setting-based mapping
            GetInheritanceAttributesByReflection(classType, out highestVersion);

        internal static SaveInheritanceAttribute[]? GetInheritanceAttributesByReflection(Type classType, out uint highestVersion)
        {
            highestVersion = 0;

            var inheritanceInfo = (SaveInheritanceAttribute[])classType.GetCustomAttributes<SaveInheritanceAttribute>(false);
            if (inheritanceInfo == null) return null;

            MappingHelpers.SortVersionedAttributes(out highestVersion, inheritanceInfo);
            return inheritanceInfo;
        }
    }
}
