using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ABCo.ABSave.Exceptions
{
    public class IncompleteDetailsException
        : ABSaveException
    {
        public IncompleteDetailsException(MemberInfo info) : 
            base($"While processing the member '{info.Name}' in type '{info.DeclaringType!.Name}', ABSave discovered that the member had other mapping attributes, but was missing the crucial 'Save' attribute. Please check the member and ensure it has the correct details attached.")
        { }
    }
}
