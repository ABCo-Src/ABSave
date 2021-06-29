using System;

namespace ABCo.ABSave.Exceptions
{
    class InvalidSubTypeInfoException : Exception
    {
        public InvalidSubTypeInfoException(Type baseType) :
            base($"While deserializing, ABSave encountered information about a sub-type '{baseType.Name}' of that didn't match up to anything in the actual type. This may be caused by accidentally modifying the inheritance info without correctly versioning it or by an invalid ABSave document.")
        { }
    }
}
