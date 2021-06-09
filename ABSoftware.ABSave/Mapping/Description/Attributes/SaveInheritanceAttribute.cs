using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Mapping.Description.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class SaveInheritanceAttribute : Attribute
    {
        public SaveInheritanceMode Mode;

        ///// <summary>
        ///// Whether to ignore any types that aren't in the list or don't have a key and simply serialize their base members.
        ///// </summary>
        //public bool IgnoreUnmarkedTypes = false;

        public Dictionary<Type, uint> IndexSerializeCache;
        public Dictionary<uint, Type> IndexDeserializeCache;

        public Dictionary<Type, string>? KeySerializeCache;
        public Dictionary<string, Type>? KeyDeserializeCache;

        public uint FromVer;
        public uint ToVer = uint.MaxValue;

        public SaveInheritanceAttribute(SaveInheritanceMode mode, params Type[] list)
        {
            Mode = mode;

            IndexSerializeCache = new Dictionary<Type, uint>();
            IndexDeserializeCache = new Dictionary<uint, Type>();

            for (uint i = 0; i < list.Length; i++)
            {
                var currentItem = list[i];
                IndexSerializeCache.Add(currentItem, i);
                IndexDeserializeCache.Add(i, currentItem);
            }
        }
    }
}
