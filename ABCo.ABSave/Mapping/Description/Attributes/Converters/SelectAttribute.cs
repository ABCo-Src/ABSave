using System;

namespace ABCo.ABSave.Mapping.Description.Attributes.Converters
{
    /// <summary>
    /// Describes a type that a converter can 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SelectAttribute : Attribute
    {
        public Type Type;
        public object[] DefiniteSubTypes;

        public SelectAttribute(Type type, params object[] definiteSubTypes)
        {
            Type = type;
            DefiniteSubTypes = definiteSubTypes;
        }
    }
}
