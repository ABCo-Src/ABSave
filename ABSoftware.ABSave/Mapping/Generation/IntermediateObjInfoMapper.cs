using ABSoftware.ABSave.Exceptions;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping.Description;
using ABSoftware.ABSave.Mapping.Description.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ABSoftware.ABSave.Mapping.Generation
{
    /// <summary>
    /// This is executed before the <see cref="ObjectMapper"/> to gather the raw data from either the attributes
    /// or externally defined maps, and then translate it all, alongside a lot of analysis data, into an
    /// intermediary form, that can then used to make the final maps for all the different version numbers.
    /// </summary>
    internal static class IntermediateObjInfoMapper
    {
        static readonly LightConcurrentPool<IntermediateObjInfo> InfoPool = new LightConcurrentPool<IntermediateObjInfo>(4);

        /// <summary>
        /// Clears the list <see cref="MapGenerator.CurrentObjMembers"/> and inserts all of the members and information attached to them.
        /// </summary>
        internal static IntermediateObjInfo CreateInfo(Type type, MapGenerator gen)
        {
            var dest = Rent();
            dest.ClassType = type;

            var ctx = new TranslationContext(dest);

            // Coming soon: Settings-based mapping
            Reflection.FillInfo(ref ctx, gen);

            dest.RawMembers = ctx.CurrentMembers.ReleaseAndGetArray();

            // Apply ordering on all the items, if necessary.
            if (ctx.TranslationCurrentOrderInfo == -1)
                OrderMembers(ref ctx);

            return dest;
        }

        internal static class Reflection
        {
            public static void FillInfo(ref TranslationContext ctx, MapGenerator gen)
            {
                var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                var classType = ctx.Destination.ClassType;
                var mode = GetClassMode(classType);

                // Get the members
                var hasFields = (mode & SaveMembersMode.Fields) > 0;
                FieldInfo[] fields = hasFields ? classType.GetFields(bindingFlags) : null;

                var hasProperties = (mode & SaveMembersMode.Properties) > 0;
                PropertyInfo[] properties = hasProperties ? classType.GetProperties(bindingFlags) : null;

                // Create our buffer
                ctx.CurrentMembers.Initialize();

                // Fields
                if (hasFields)
                    for (int i = 0; i < fields.Length; i++)
                        AddItem(ref ctx, gen, fields[i], fields[i].FieldType);

                // Properties
                if (hasProperties)
                    for (int i = 0; i < properties.Length; i++)
                    {
                        if (!properties[i].CanRead || !properties[i].CanWrite) continue;
                        AddItem(ref ctx, gen, properties[i], properties[i].PropertyType);
                    }
            }

            static void AddItem(ref TranslationContext ctx, MapGenerator gen, MemberInfo info, Type infoType)
            {
                var attributes = (MapAttr[])info.GetCustomAttributes(typeof(MapAttr), false);
                if (attributes.Length == 0) return;

                ref ObjectTranslatedItemInfo currentItem = ref ctx.CurrentMembers.CreateAndGet();

                // Fill in the basic details
                FillSharedItemInfo(ref ctx, ref currentItem, gen, info, infoType);

                // Go through the attributes and fill that info in.
                FillAttributeInfo(ref ctx, ref currentItem, attributes, info);
            }

            static SaveMembersMode GetClassMode(Type classType)
            {
                // TODO: This is jsut to temporarily support "object" until proper settings mapping comes in.
                if (classType == typeof(object)) return SaveMembersMode.Fields;

                var attribute = classType.GetCustomAttribute<SaveMembersAttribute>(false);
                if (attribute == null) throw new ABSaveUnserializableType(classType);

                return attribute.Mode;
            }

            static void FillAttributeInfo(ref TranslationContext ctx, ref ObjectTranslatedItemInfo currentItem, MapAttr[] attributes, MemberInfo info)
            {
                // Interpret the attributes.
                bool loadedSaveAttribute = false;
                for (int i = 0; i < attributes.Length; i++)
                {
                    switch (attributes[i])
                    {
                        case SaveAttribute save:
                            FillMainInfo(ref ctx, ref currentItem, save.Order, save.FromVer, save.ToVer);
                            loadedSaveAttribute = true;
                            break;
                    }
                }

                if (!loadedSaveAttribute) throw new ABSaveIncompleteDetailsException(info);
            }
        }

        internal static void FillMainInfo(ref TranslationContext ctx, ref ObjectTranslatedItemInfo currentItem, int order, int startVer, int endVer)
        {
            currentItem.Order = order;

            // Check ordering
            if (ctx.TranslationCurrentOrderInfo != -1)
            {
                if (order >= ctx.TranslationCurrentOrderInfo)
                    ctx.TranslationCurrentOrderInfo = order;
                else
                    ctx.TranslationCurrentOrderInfo = -1;
            }

            var dest = ctx.Destination;

            // If the version given is -1, that means it doesn't have a set end, so we'll just fill that in with "uint.MaxValue".
            // In this situation we'll only update the highest version based on what the minimum is.
            if (endVer == -1)
            {
                (currentItem.StartVer, currentItem.EndVer) = (checked((uint)startVer), uint.MaxValue);

                if (startVer > dest.HighestVersion)
                    dest.HighestVersion = startVer;
            }
                

            // If not, place it in as is, and track what the highest version is based on what their custom high is.
            else
            {
                (currentItem.StartVer, currentItem.EndVer) = (checked((uint)startVer), checked((uint)endVer));

                if (endVer > dest.HighestVersion)
                    dest.HighestVersion = endVer;
            }
        }

        static void FillSharedItemInfo(ref TranslationContext ctx, ref ObjectTranslatedItemInfo currentItem, MapGenerator gen, MemberInfo info, Type memberType)
        {
            currentItem.Info = info;
            currentItem.MemberType = memberType;

            // Handle mapping
            if (MapGenerator.TryGetItemFromDict(gen.Map.GenInfo.AllTypes, memberType, MapItemState.Planned, out MapItemInfo val))
                currentItem.ExistingMap = val;
            else
                ctx.Destination.UnmappedCount++;
        }

        internal static void OrderMembers(ref TranslationContext ctx)
        {
            var dest = ctx.Destination;

            // Create information about what order all the members should be.
            dest.SortedMembers = new ObjectTranslatedSortInfo[dest.RawMembers.Length];

            for (int i = 0; i < dest.RawMembers.Length; i++)
            {
                dest.SortedMembers[i].Order = dest.RawMembers[i].Order;
                dest.SortedMembers[i].Index = (short)i;
            }

            Array.Sort(dest.SortedMembers);
        }

        internal static bool IsField(MapGenerator gen)
        {
            return gen.Map.Settings.ConvertFields;
        }


        static IntermediateObjInfo Rent()
        {
            var pooled = InfoPool.TryRent();
            if (pooled == null) return new IntermediateObjInfo();

            pooled.UnmappedCount = 0;
            pooled.HighestVersion = 0;
            pooled.ClassType = null;
            pooled.RawMembers = null;
            pooled.SortedMembers = null;
            return pooled;
        }

        public static void Release(IntermediateObjInfo dest)
        {
            // Null these out so they can be collected and aren't being held alive.
            dest.ClassType = null;
            dest.RawMembers = null;
            dest.SortedMembers = null;
            InfoPool.Release(dest);
        }

        internal struct TranslationContext
        {
            public LoadOnceList<ObjectTranslatedItemInfo> CurrentMembers;
            public IntermediateObjInfo Destination;
            public int TranslationCurrentOrderInfo;

            public TranslationContext(IntermediateObjInfo dest)
            {
                Destination = dest;
                CurrentMembers = new LoadOnceList<ObjectTranslatedItemInfo>();
                TranslationCurrentOrderInfo = 0;
            }
        }
    }
}
