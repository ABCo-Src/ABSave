using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Exceptions
{
    public abstract class ABSaveException : Exception
    {
        public ABSaveException(string msg) : base(msg) { }
    }
}
