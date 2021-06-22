using System;

namespace ABCo.ABSave.Exceptions
{
    public abstract class ABSaveException : Exception
    {
        public ABSaveException(string msg) : base(msg) { }
    }
}
