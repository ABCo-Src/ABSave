using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Exceptions
{
    public class InvalidSaveAttributeSetException : ABSaveException
    {
        public InvalidSaveAttributeSetException(Type type) 
            : base($"ABSave detected that the class '{type.Name}' contains a member with some overlapping 'Save' attributes that apply to multiple versions. Please ensure that if you have multiple 'Save' attribute on a member none of them 'apply' to the same version")
        { }
    }
}
