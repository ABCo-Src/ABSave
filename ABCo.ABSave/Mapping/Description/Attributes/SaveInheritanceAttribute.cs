using System;
using System.Collections.Generic;

namespace ABCo.ABSave.Mapping.Description.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class SaveInheritanceAttribute : AttributeWithVersion
    {
        public SaveInheritanceMode Mode;
        internal bool HasGeneratedFullKeyCache;

        internal Dictionary<Type, uint> IndexSerializeCache;
        internal Dictionary<uint, Type> IndexDeserializeCache;

        internal Dictionary<Type, string>? KeySerializeCache;
        internal Dictionary<string, Type>? KeyDeserializeCache;

        public SaveInheritanceAttribute(SaveInheritanceMode mode, params Type[] list)
        {
            Mode = mode;

            IndexSerializeCache = new Dictionary<Type, uint>();
            IndexDeserializeCache = new Dictionary<uint, Type>();

            for (uint i = 0; i < list.Length; i++)
            {
                Type? currentItem = list[i];
                IndexSerializeCache.Add(currentItem, i);
                IndexDeserializeCache.Add(i, currentItem);
            }
        }
    }
}
