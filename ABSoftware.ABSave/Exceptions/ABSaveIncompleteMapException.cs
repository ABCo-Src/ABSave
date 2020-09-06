using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Exceptions
{
    public class ABSaveIncompleteMapException : Exception
    {
        public ABSaveIncompleteMapException() : base("An object in the given ABSave document has more items than are provided in the map.") { }
    }
}
