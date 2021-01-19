using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Exceptions
{
    public class ABSaveUnrecognizedCollectionException : ABSaveException
    {
        public ABSaveUnrecognizedCollectionException() : base("ABSave does not support the collection given. Keep in mind that ABSave cannot serialize plain IEnumerables.") { }
    }
}
