using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Exceptions
{
    public class NullDictionaryKeyException : ABSaveException
    {
        public NullDictionaryKeyException() : base("ABSave encountered an 'null' dictionary key in the document. ABSave does not support null dictionary keys. It's possible this may have been caused by an invalid document") { }
    }
}
