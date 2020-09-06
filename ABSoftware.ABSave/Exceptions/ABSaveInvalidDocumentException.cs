using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Exceptions
{
    public class ABSaveInvalidDocumentException : Exception
    {
        public ABSaveInvalidDocumentException(long pos) : base($"The document given is invalid. Failed to read at position '{pos}'.") { }
    }
}
