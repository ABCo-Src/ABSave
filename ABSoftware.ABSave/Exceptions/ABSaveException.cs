using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Exceptions
{
    public abstract class ABSaveException : Exception
    {
        public ABSaveException(string msg) : base(msg) { }
    }
}
