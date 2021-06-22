using ABCo.ABSave.Mapping.Description.Attributes;
using System;
/* Unmerged change from project 'ABCo.ABSave (net5.0)'
Before:
using System.Collections.Generic;
After:
using System.Reflection;
*/


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
                    var info = inheritanceInfo[i];
                    MappingHelpers.UpdateHighestVersionFromRange(ref highestVersion, info.FromVer, info.ToVer);
                }
            }

            return inheritanceInfo;
        }
    }
}
