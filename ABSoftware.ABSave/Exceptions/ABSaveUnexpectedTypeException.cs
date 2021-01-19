using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Exceptions
{
    public class ABSaveUnexpectedTypeException : ABSaveException
    {
        public ABSaveUnexpectedTypeException(Type expected, Type actual) : base($"While deserializing, ABSave encountered a type, {expected.FullName}, that was not of the required base type, {actual.FullName}. ABSave, by default, only allows types that are sub-classes of the expected base as a security measure against attackers. See the configuration section of the documentation for more information. If you know the ABSave is valid, it's possible that a class change has caused this, in which case you must ensure you follow 'versionability' guidelines in the documentation.") { }
    }
}
