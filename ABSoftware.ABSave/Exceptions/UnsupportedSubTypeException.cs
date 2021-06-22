using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Exceptions
{
    public class UnsupportedSubTypeException : ABSaveException
    {
        public UnsupportedSubTypeException(Type baseType, Type actual) : 
            base($"While converting, ABSave encountered an instance of type {actual.FullName} in a place of type {baseType.FullName}, however the type {baseType.FullName} doesn't allow this type anywhere within its inheritance info.") { }
    }
}
