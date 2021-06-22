using ABCo.ABSave.Mapping.Description.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Mapping.Generation.Inheritance
{
    public static class InheritanceHandler
    {
        internal static SaveInheritanceAttribute? GetAttributeForVersion(SaveInheritanceAttribute[]? attributes, uint version)
        {
            if (attributes == null) return null;

            for (int i = 0; i < attributes.Length; i++)
            {
                var currentAttribute = attributes[i];
                if (currentAttribute.FromVer <= version && currentAttribute.ToVer >= version)
                    return currentAttribute;
            }

            return null;
        }

        internal static SaveInheritanceAttribute[]? GetInheritanceAttributes(Type classType, ref uint highestVersion)
        {
            // Coming soon: Setting-based mapping
            return InheritanceReflectionGenerator.GetInheritanceAttributes(classType, ref highestVersion);
        }
    }
}
