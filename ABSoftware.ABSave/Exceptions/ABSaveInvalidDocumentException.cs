using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Exceptions
{
    public class ABSaveInvalidDocumentException : ABSaveException
    {
        public ABSaveInvalidDocumentException(string msg) : base($"The document given is invalid. " + msg) { }
    }
}
