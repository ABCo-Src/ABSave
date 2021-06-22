using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Exceptions
{
    public class InvalidDictionaryException : ABSaveException
    {
        public InvalidDictionaryException(Type typeName) : base($"ABSave cannot convert a dictionary of type '{typeName.Name}'. ABSave can only convert dictionaries that provide an 'IDictionaryEnumerator' via 'GetEnumerator'. Consider making a custom converter for the type if you cannot change this.") { }
    }
}
