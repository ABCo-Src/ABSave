using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Mapping.Description.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class SaveInheritanceKeyAttribute : Attribute
    {
        public string Key;
        public SaveInheritanceKeyAttribute(string key)
        {
            if (key == "") throw new Exception("The key used cannot be empty.");
            Key = key;
        }
    }
}
