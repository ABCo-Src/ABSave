using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Helpers
{
    public struct CollectionDetails
    {
        public IABSaveEnumerableInfo Info;
        public Type ElementTypeOrKeyType;
        public Type ValueType;

        public CollectionDetails(IABSaveEnumerableInfo info, Type elementTypeOrKeyType, Type valueType)
        {
            Info = info;
            ElementTypeOrKeyType = elementTypeOrKeyType;
            ValueType = valueType;
        }
    }
}
