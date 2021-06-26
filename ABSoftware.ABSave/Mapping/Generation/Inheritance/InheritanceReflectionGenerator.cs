using ABCo.ABSave.Mapping.Description.Attributes;
using System;
using System.Reflection;

namespace ABCo.ABSave.Mapping.Generation.Inheritance
{
    internal static class InheritanceReflectionGenerator
    {
        public static SaveInheritanceAttribute[]? GetInheritanceAttributes(Type classType, ref uint highestVersion)
        {
            var inheritanceInfo = (SaveInheritanceAttribute[])classType.GetCustomAttributes<SaveInheritanceAttribute>(false);

            if (inheritanceInfo.Length == 0)
                inheritanceInfo = null;
            else
            {
                for (int i = 0; i < inheritanceInfo.Length; i++)
                {
                    SaveInheritanceAttribute? info = inheritanceInfo[i];
                    MappingHelpers.UpdateHighestVersionFromRange(ref highestVersion, (uint)info.FromVer, (uint)info.ToVer);
                }
            }

            return inheritanceInfo;
        }
    }
}
