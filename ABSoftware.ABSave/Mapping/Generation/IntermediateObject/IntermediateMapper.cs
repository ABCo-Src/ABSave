using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Mapping.Generation.IntermediateObject
{
    // This is created first before an object map is made. This gathers the raw data from either the
    // attributes or externally defined maps, and then translate it all, alongside a lot of analysis data, into an
    // intermediary form, that can then used to make the final maps for all the different version numbers.
    internal static class IntermediateMapper
    {
        // Context used while creating intermediate info.
        internal static TranslationContext _intermediateContext = new TranslationContext();

        /// <summary>
        /// Clears the list <see cref="MapGenerator.CurrentObjMembers"/> and inserts all of the members and information attached to them.
        /// </summary>
        internal uint CreateIntermediateObjectInfo(Type type, ref ObjectIntermediateInfo info)
        {
            _intermediateContext = new TranslationContext(type);

            // Coming soon: Settings-based mapping
            var members = IntermediateReflectionInfo.FillInfo(out SaveInheritanceAttribute[]? attr);

            if (_intermediateContext.TranslationCurrentOrderInfo == -1)
                Array.Sort(members);

            info = new ObjectIntermediateInfo(members, attr);
            return _intermediateContext.HighestVersion;
        }

        internal class ReflectionMapper
        {
            internal MapGenerator Gen;
            internal ReflectionMapper(MapGenerator gen)
            {
                Gen = gen;
                _threadPoolAddItem = ProcessAttributesOnThreadPool;
            }


        }

        internal static void FillMainInfo(ObjectIntermediateItem newItem, int order, int startVer, int endVer)
        {
            newItem.Order = order;

            // If the version given is -1, that means it doesn't have a set end, so we'll just fill that in with "uint.MaxValue".            
            newItem.StartVer = checked((uint)startVer);
            newItem.EndVer = endVer == -1 ? uint.MaxValue : checked((uint)endVer);
        }

        internal void UpdateContextFromItem(ObjectIntermediateItem newItem)
        {
            // Check ordering
            if (_intermediateContext.TranslationCurrentOrderInfo != -1)
            {
                if (newItem.Order >= _intermediateContext.TranslationCurrentOrderInfo)
                    _intermediateContext.TranslationCurrentOrderInfo = newItem.Order;
                else
                    _intermediateContext.TranslationCurrentOrderInfo = -1;
            }

            UpdateVersionInfo(newItem.StartVer, newItem.EndVer);
        }

        void UpdateVersionInfo(uint startVer, uint endVer)
        {
            // If there is no upper we'll only update the highest version based on what the minimum is.
            if (endVer == uint.MaxValue)
            {
                if (startVer > _intermediateContext.HighestVersion)
                    _intermediateContext.HighestVersion = startVer;
            }

            // If not update based on what their custom high is.
            else
            {
                if (endVer > _intermediateContext.HighestVersion)
                    _intermediateContext.HighestVersion = endVer;
            }
        }

        internal struct TranslationContext
        {
            public Type ClassType;
            public int TranslationCurrentOrderInfo;

            // Used to count how many unskipped members were present so we know the size for our final array.
            public int UnskippedMemberCount;
            public uint HighestVersion;

            public TranslationContext(Type classType)
            {
                ClassType = classType;
                TranslationCurrentOrderInfo = 0;
                UnskippedMemberCount = 0;
                HighestVersion = 0;
            }
        }

        internal struct TranslationContext
        {
            public Type ClassType;
            public int TranslationCurrentOrderInfo;

            // Used to count how many unskipped members were present so we know the size for our final array.
            public int UnskippedMemberCount;
            public uint HighestVersion;

            public TranslationContext(Type classType)
            {
                ClassType = classType;
                TranslationCurrentOrderInfo = 0;
                UnskippedMemberCount = 0;
                HighestVersion = 0;
            }
        }
    }
}
