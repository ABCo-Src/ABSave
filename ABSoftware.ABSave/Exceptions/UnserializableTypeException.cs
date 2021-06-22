using System;

namespace ABCo.ABSave.Exceptions
{
    public class UnserializableTypeException : Exception
    {
        public UnserializableTypeException(Type type)
            : base($"Unable to create a map for the type, because the type '{type.Name}' does not have a converter, and is not marked with the 'SaveMembers' attribute. " +
                  $"If you want ABSave to serialize all the individual members of a type, you must remember to mark it with this attribute. If you are unable to add the attribute to the type, you may add the type to the settings manually, see the 'mapping' documentation for more information.")
        { }
    }
}
