using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ABCo.ABSave.Exceptions
{
    public class InvalidAttributeToVerException : ABSaveException
    {
        public InvalidAttributeToVerException() : base($"An attribute in the map has a 'ToVer' that is either the same as or below 'FromVer'. Please remember that 'ToVer' is exclusive and as such a 'FromVer' of '2' and 'ToVer' of '3' will target version 2 only, while a 'FromVer' of '2' and 'ToVer' of '2' would hypothetically target zero, which is invalid.") { }
    }
}
