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

namespace ABSoftware.ABSave.Mapping.Generation
{
    // This is executed before the "Object" section to gather the raw data from either the attributes
    // or externally defined maps, and then translate it all, alongside a lot of analysis data, into an
    // intermediary form, that can then used to make the final maps for all the different version numbers.    
    public partial class MapGenerator
    {
        // Constant values used in the buffer below
        internal static readonly ObjectIntermediateItem _skipMapItem = new ObjectIntermediateItem();
        internal static readonly ObjectIntermediateItem _invalidMapItem = new ObjectIntermediateItem();

        // A buffer of the current members as we add them in the intermediate items.
        // This will contain "null"s at first but those will be turned into
        // the correct thing as they get processed on the thread pool.
        //
        // If an item becomes "_skipMapItem", that means that particular item should be ignored
        // as it did not have the "Save" attributes.
        //
        // If there was a problem with the attributes on the item (i.e. contained some but not required)
        // "_invalidMapItem" will be set and once seen this will call the system to throw
        internal ObjectIntermediateItem?[] _intermediateCurrentBuffer = Array.Empty<ObjectIntermediateItem?>();
        internal int _intermediateCurrentBufferLength;

        // Context used while creating intermediate info.
        internal TranslationContext _intermediateContext = new TranslationContext();

        /// <summary>
        /// Clears the list <see cref="MapGenerator.CurrentObjMembers"/> and inserts all of the members and information attached to them.
        /// </summary>
        internal IntermediateObjInfo CreateIntermediateObjectInfo(Type type)
        {
            _intermediateContext = new TranslationContext(type);

            // Coming soon: Settings-based mapping
            var members = Reflection.FillInfo(this);

            if (_intermediateContext.TranslationCurrentOrderInfo == -1)
                Array.Sort(members);

            return new IntermediateObjInfo(_intermediateContext.HighestVersion, members);
        }

        internal class Reflection
        {
            public static ObjectIntermediateItem[] FillInfo(MapGenerator gen)
            {
                var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                var classType = gen._intermediateContext.ClassType;
                var mode = GetClassMode(classType);

                // Get the members
                FieldInfo[] fields = GetFields(bindingFlags, classType, mode);
                PropertyInfo[] properties = GetProperties(bindingFlags, classType, mode);

                PrepareBuffer(fields.Length + properties.Length, gen);

                // Process the members
                if (fields.Length > 0)
                    ProcessMembers(gen, fields, 0, fields.Length);
                if (properties.Length > 0)
                    ProcessMembers(gen, properties, fields.Length, properties.Length);

                // Put it all together to make the final member array.
                return CreateFinalArrayFromBuffer(gen);
            }

            static void ProcessMembers(MapGenerator gen, MemberInfo[] items, int offset, int length)
            {
                // First, we'll go through every member, and start examining all their attributes 
                // on the thread pool in the background.
                //
                // And then we'll go through again and process all the ones that have been examined.
                if (offset == 0)
                {
                    for (int i = 0; i < length; i++)
                        StartAddItem(gen, items[i], i);
                    for (int i = 0; i < length; i++)
                        FinishAddItem(gen, items[i], i);
                }
                else
                {
                    for (int i = 0; i < length; i++)
                        StartAddItem(gen, items[i], offset + i);
                    for (int i = 0; i < length; i++)
                        FinishAddItem(gen, items[i], offset + i);
                }
            }

            static void StartAddItem(MapGenerator gen, MemberInfo info, int idx)
            {
                // Queue up getting the attributes to the thread pool.
                ThreadPool.QueueUserWorkItem(ProcessItemAttributes, new ThreadItemInfo(info, gen, idx), false);
            }

            static void FinishAddItem(MapGenerator gen, MemberInfo info, int idx)
            {
                ref ObjectIntermediateItem? item = ref gen._intermediateCurrentBuffer[idx];

                // TODO: Look into perhaps a built in way to poll this?
                while (Volatile.Read(ref item) == null)
                    Thread.Yield();

                if (item == _skipMapItem)
                {
                    item = null;
                    return;
                }
                else if (item == _invalidMapItem)
                {
                    // Before we throw we need to wait for all the tasks to finish - else we'll risk
                    // using the MapGenerator again for something else while thread pool
                    // is still using the buffer!
                    WaitForFullCompletion(gen, idx);
                    throw new IncompleteDetailsException(info);
                }
                else
                {
                    gen.UpdateContextFromItem(item!);
                    gen._intermediateContext.UnskippedMemberCount++;
                }
            }

            internal static void ProcessItemAttributes(ThreadItemInfo info)
            {
                ref ObjectIntermediateItem? dest = ref info.Gen._intermediateCurrentBuffer[info.Idx];

                var attributes = info.Info.GetCustomAttributes(typeof(MapAttr), true);
                if (attributes.Length == 0)
                    Volatile.Write(ref dest, _skipMapItem); // Mark it to be skipped.

                var newItem = new ObjectIntermediateItem();
                newItem.Details.Unprocessed = info.Info;

                bool successful = InterpretAttributes(newItem, attributes);
                Volatile.Write(ref dest, successful ? newItem : _skipMapItem);
            }

            private static bool InterpretAttributes(ObjectIntermediateItem dest, object[] attributes)
            {
                bool loadedSaveAttribute = false;
                for (int i = 0; i < attributes.Length; i++)
                {
                    switch (attributes[i])
                    {
                        case SaveAttribute save:
                            FillMainInfo(dest, save.Order, save.FromVer, save.ToVer);
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

            static void PrepareBuffer(int size, MapGenerator gen)
            {
                if (gen.PrepareBufferForSize(size))
                    gen.ClearBuffer();
            }

            private static void WaitForFullCompletion(MapGenerator gen, int idx)
            {
                for (int i = idx + 1; i < gen._intermediateCurrentBuffer.Length; i++)
                {
                    ref ObjectIntermediateItem? eachItem = ref gen._intermediateCurrentBuffer[idx];
                    while (Volatile.Read(ref eachItem) == null);
                }
            }

            static ObjectIntermediateItem[] CreateFinalArrayFromBuffer(MapGenerator gen)
            {
                int arrPos = 0;
                var arr = new ObjectIntermediateItem[gen._intermediateContext.UnskippedMemberCount];
                var buffer = gen._intermediateCurrentBuffer;
                int len = gen._intermediateCurrentBufferLength;

                for (int i = 0; i < len; i++)
                    if (buffer[i] != null)
                        arr[arrPos++] = buffer[i]!;

                return arr;
            }

            // Info handed over to each thread pool operation.
            internal struct ThreadItemInfo
            {
                public MemberInfo Info;
                public MapGenerator Gen;
                public int Idx;

                public ThreadItemInfo(MemberInfo info, MapGenerator gen, int idx) => (Info, Gen, Idx) = (info, gen, idx);
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

        internal void ClearBuffer()
        {
            Array.Clear(_intermediateCurrentBuffer, 0, _intermediateCurrentBuffer.Length);
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
