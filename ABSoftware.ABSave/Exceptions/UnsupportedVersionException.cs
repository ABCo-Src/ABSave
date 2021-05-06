using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Exceptions
{
    public class UnsupportedVersionException : ABSaveException
    {
        public UnsupportedVersionException(Type type, int version) : 
            base($"It is most likely that the ABSave given was created in a newer version of this application. In the given ABSave, the type '{type.Name}' was given with the version number '{version}'. However, this version is not defined on the application's class.") { }
    }
}
