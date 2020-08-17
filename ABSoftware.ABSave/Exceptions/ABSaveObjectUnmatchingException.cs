using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Exceptions
{
    public class ABSaveObjectUnmatchingException : Exception
    {
        public ABSaveObjectUnmatchingException() : base("The object data in the ABSave document and the actual target object are different. If you want items that no longer exist to be ignored, disable 'ErrorOnUnknownItem' in the settings.") { }
    }
}
