using ABCo.ABSave.Mapping.Description.Attributes;
using System;

namespace ABCo.ABSave.Mapping.Generation.Inheritance
{
    public static class InheritanceHandler
    {
        internal static SaveInheritanceAttribute[]? GetInheritanceAttributes(Type classType, ref uint highestVersion) =>
            // Coming soon: Setting-based mapping
            InheritanceReflectionGenerator.GetInheritanceAttributes(classType, ref highestVersion);
    }
}
