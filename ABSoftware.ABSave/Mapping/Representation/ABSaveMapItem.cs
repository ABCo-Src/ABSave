using ABSoftware.ABSave.Converters;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave.Mapping.Representation
{
    public abstract class ABSaveMapItem
    {
        internal readonly object AccessLock = new object();

        /// <summary>
        /// The number of times this map has been used, allows us to optimize commonly used types with fast IL.
        /// </summary>
        internal int UsageCount = 0;

        public Action<object, ABSaveWriter> FastSerializeIL;

        public abstract void SerializeData(object obj, Type type, ABSaveWriter writer);
    }
}
