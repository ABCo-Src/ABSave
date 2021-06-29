using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Exceptions
{
    public class InvalidSaveBaseMembersException : ABSaveException
    {
        public InvalidSaveBaseMembersException(string message) : base(message) { }
    }
}
