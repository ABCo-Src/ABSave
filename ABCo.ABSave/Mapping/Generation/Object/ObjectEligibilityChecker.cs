using ABCo.ABSave.Mapping.Description;
using ABCo.ABSave.Mapping.Description.Attributes;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Mapping.Generation.Object
{
    /// <summary>
    /// In charge of determining whether a given type is eligible to be object converted.
    /// (i.e. it has an entry in the setting or a <see cref="SaveMembersAttribute"/>.)
    /// </summary>
    public static class ObjectEligibilityChecker
    {
        // Coming soon: Settings-based mapping
        public static bool CheckIfEligibleAndGetSaveMode(Type type, out SaveMembersMode mode)
        {
            mode = SaveMembersMode.Fields;

            SaveMembersAttribute? attribute = type.GetCustomAttribute<SaveMembersAttribute>(false);
            if (attribute == null) return false;

            mode = attribute.Mode;
            return true;
        }

        public static bool IsEligible(Type type) => Attribute.IsDefined(type, typeof(SaveMembersAttribute));
    }
}
