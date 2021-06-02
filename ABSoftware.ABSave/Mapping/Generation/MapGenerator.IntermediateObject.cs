using ABSoftware.ABSave.Exceptions;
using ABSoftware.ABSave.Mapping.Description;
using ABSoftware.ABSave.Mapping.Description.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ABSoftware.ABSave.Mapping.Generation
{
    // This is executed before the "Object" section to gather the raw data from either the attributes
    // or externally defined maps, and then translate it all, alongside a lot of analysis data, into an
    // intermediary form, that can then used to make the final maps for all the different version numbers.    
    public partial class MapGenerator
    {
        public static bool IsParallel = false;

        internal ReflectionMapper CurrentReflectionMapper;

        // Context used while creating intermediate info.
        internal TranslationContext _intermediateContext = new TranslationContext();

        /// <summary>
        /// Clears the list <see cref="MapGenerator.CurrentObjMembers"/> and inserts all of the members and information attached to them.
        /// </summary>
        internal IntermediateObjInfo CreateIntermediateObjectInfo(Type type)
        {
            _intermediateContext = new TranslationContext(type);

            // Coming soon: Settings-based mapping
            var members = CurrentReflectionMapper.FillInfo();

            if (_intermediateContext.TranslationCurrentOrderInfo == -1)
                Array.Sort(members);

            return new IntermediateObjInfo(_intermediateContext.HighestVersion, members);
        }

        internal class ReflectionMapper
        {
            internal MapGenerator Gen;
            internal ReflectionMapper(MapGenerator gen)
            { 
                Gen = gen;
                _threadPoolAddItem = ProcessAttributesOnThreadPool;
            }

            // Constant values used in the buffer below
            internal static readonly ObjectIntermediateItem _skipMapItem = new ObjectIntermediateItem();
            internal static readonly ObjectIntermediateItem _invalidMapItem = new ObjectIntermediateItem();

            // A buffer used as we're processing the attributes of each member in parallel, each item goes to 
            // one member. Any null items are members that did not have the "Save" attribute and should be ignored.
            internal ObjectIntermediateItem?[] _intermediateCurrentBuffer = Array.Empty<ObjectIntermediateItem?>();
            internal int _intermediateCurrentBufferLength;

            internal MemberInfo[] _currentFields = Array.Empty<FieldInfo>();
            internal MemberInfo[] _currentProperties = Array.Empty<PropertyInfo>();

            public ObjectIntermediateItem[] FillInfo()
            {
                var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                var classType = Gen._intermediateContext.ClassType;
                var mode = GetClassMode(classType);

                // Get the members
                _currentFields = GetFields(bindingFlags, classType, mode);
                _currentProperties = GetProperties(bindingFlags, classType, mode);

                PrepareBuffer(_currentFields.Length + _currentProperties.Length);

                // Process the members
                return ProcessMembers();
            }

            // Info used by the threads while interpreting the attributes on members.
            static readonly ParallelOptions Options = new ParallelOptions() { MaxDegreeOfParallelism = 2 };

            ObjectIntermediateItem[] ProcessMembers()
            {
                // First, we'll go through every member, and start examining all their attributes in parallel.
                // As we do this, we'll also count how many members do have the "SaveAttribute".
                int count = ParallelProcessMembers();

                // And then we'll go through what's now in the buffer, make the final members array, move it
                // into that new array and update the context based on what we see as we do it.
                return ProcessBufferContents(count);
            }

            volatile ManualResetEventSlim _finishedThreadPoolWork = new ManualResetEventSlim();
            int _threadPoolCount;

            int ParallelProcessMembers()
            {
                _threadPoolCount = 0;
                _finishedThreadPoolWork.Reset();

                // Unfortunately the overhead of anything such as "Parallel" completely negates the benefit,
                // as do any fancy queues of what's left to process. We need to keep the parallelism extremely simple.
                // So literally all we'll do is do one half on the thread pool, and the other half here.
                ThreadPoolQueue();

                int localCount = 0;
                ProcessMembers(0, ref localCount);

                _finishedThreadPoolWork.Wait();
                return localCount + _threadPoolCount;
            }

            void ThreadPoolQueue()
            {
#if NET5_0_OR_GREATER
                ThreadPool.UnsafeQueueUserWorkItem(_threadPoolAddItem, null, true);
#else
                ThreadPool.UnsafeQueueUserWorkItem(_threadPoolAddItem, null);
#endif
            }

            ObjectIntermediateItem[] ProcessBufferContents(int totalCount)
            {
                int resPos = 0;
                var res = new ObjectIntermediateItem[totalCount];

                for (int i = 0; i < _intermediateCurrentBufferLength; i++)
                {
                    ObjectIntermediateItem? item = _intermediateCurrentBuffer[i];
                    if (item == null) continue;

                    Gen.UpdateContextFromItem(item);
                    res[resPos++] = item;
                }

                return res;
            }


            readonly
#if NET5_0_OR_GREATER
                Action<object?>
#else
                WaitCallback
#endif
                _threadPoolAddItem;

            private void ProcessAttributesOnThreadPool(object? state)
            {
                ProcessMembers(1, ref _threadPoolCount);
                _finishedThreadPoolWork.Set();
            }

            void ProcessMembers(int offset, ref int count)
            {
                for (int i = offset; i < _currentFields.Length; i += 2)
                    ProcessMemberAttributes(_currentFields[i], ref _intermediateCurrentBuffer[i], ref count);

                int currentBufferOffset = _currentFields.Length;

                if (currentBufferOffset == 0)
                    for (int i = offset; i < _currentProperties.Length; i += 2)
                        ProcessMemberAttributes(_currentProperties[i], ref _intermediateCurrentBuffer[i], ref count);
                else
                    for (int i = offset; i < _currentProperties.Length; i += 2)
                    {
                        ProcessMemberAttributes(_currentProperties[i], ref _intermediateCurrentBuffer[i], ref count);
                        currentBufferOffset += 2;
                    }
            }

            internal static void ProcessMemberAttributes(MemberInfo info, ref ObjectIntermediateItem? dest, ref int count)
            {
                // Get the attributes - skip this item if there are none
                var attributes = info.GetCustomAttributes(typeof(MapAttr), true);
                if (attributes.Length == 0) return;

                // Create the item.
                bool successful = CreateItemFromAttributes(out ObjectIntermediateItem newItem, info, attributes);
                if (!successful) throw new IncompleteDetailsException(info);

                dest = newItem;
                count++;
            }

            private static bool CreateItemFromAttributes(out ObjectIntermediateItem newItem, MemberInfo info, object[] attributes)
            {
                newItem = new ObjectIntermediateItem();
                newItem.Details.Unprocessed = info;

                bool loadedSaveAttribute = false;
                for (int i = 0; i < attributes.Length; i++)
                {
                    switch (attributes[i])
                    {
                        case SaveAttribute save:
                            FillMainInfo(newItem, save.Order, save.FromVer, save.ToVer);
                            loadedSaveAttribute = true;
                            break;
                    }
                }

                return loadedSaveAttribute;
            }

            static SaveMembersMode GetClassMode(Type classType)
            {
                // TODO: This is just to temporarily support "object" until proper settings mapping comes in.
                if (classType == typeof(object)) return SaveMembersMode.Fields;

                var attribute = classType.GetCustomAttribute<SaveMembersAttribute>(false);
                if (attribute == null) throw new UnserializableType(classType);

                return attribute.Mode;
            }

            static FieldInfo[] GetFields(BindingFlags bindingFlags, Type classType, SaveMembersMode mode)
            {
                var fields = Array.Empty<FieldInfo>();

                if ((mode & SaveMembersMode.Fields) > 0)
                    fields = classType.GetFields(bindingFlags);

                return fields;
            }

            static PropertyInfo[] GetProperties(BindingFlags bindingFlags, Type classType, SaveMembersMode mode)
            {
                var properties = Array.Empty<PropertyInfo>();

                if ((mode & SaveMembersMode.Properties) > 0)
                    properties = classType.GetProperties(bindingFlags);

                return properties;
            }

            void PrepareBuffer(int size)
            {
                if (PrepareBufferForSize(size))
                    ClearBuffer();
            }

            // Returns: Needs clearing before use
            internal bool PrepareBufferForSize(int requiredSize)
            {
                _intermediateCurrentBufferLength = requiredSize;
                if (_intermediateCurrentBuffer.Length < requiredSize)
                {
                    _intermediateCurrentBuffer = new ObjectIntermediateItem[requiredSize];
                    return false;
                }

                return true;
            }

            internal void ClearBuffer() => 
                Array.Clear(_intermediateCurrentBuffer, 0, _intermediateCurrentBuffer.Length);

            // Local info handed over to the threads.
            internal struct ThreadLocalInfo
            {
                public MapGenerator Gen;
                public int Offset;

                public ThreadLocalInfo(MapGenerator gen, int idxOffset) => (Gen, Offset) = (gen, idxOffset);
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

            // If there is no upper we'll only update the highest version based on what the minimum is.
            if (newItem.EndVer == uint.MaxValue)
            {
                if (newItem.StartVer > _intermediateContext.HighestVersion)
                    _intermediateContext.HighestVersion = newItem.StartVer;
            }

            // If not update based on what their custom high is.
            else
            {
                if (newItem.EndVer > _intermediateContext.HighestVersion)
                    _intermediateContext.HighestVersion = newItem.EndVer;
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
