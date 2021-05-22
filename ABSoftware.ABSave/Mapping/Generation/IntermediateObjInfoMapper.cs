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
        /// <summary>
        /// Clears the list <see cref="MapGenerator.CurrentObjMembers"/> and inserts all of the members and information attached to them.
        /// </summary>
        internal static IntermediateObjInfo CreateInfo(Type type, MapGenerator gen)
        {
            var ctx = new TranslationContext(type);

            // Coming soon: Settings-based mapping
            Reflection.FillInfo(ref ctx, gen);

            var rawMembers = ctx.CurrentMembers.ToArray();

            // Order all the items, if necessary.
            if (ctx.TranslationCurrentOrderInfo == -1)
                Array.Sort(rawMembers);

            return new IntermediateObjInfo(ctx.HighestVersion, rawMembers);
        }

        internal static class Reflection
        {
            public static void FillInfo(ref TranslationContext ctx, MapGenerator gen)
            {
                var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                var mode = GetClassMode(ctx.ClassType);

                // Fields
                if ((mode & SaveMembersMode.Fields) > 0)
                {
                    var fields = ctx.ClassType.GetFields(bindingFlags);

                    for (int i = 0; i < fields.Length; i++)
                        AddItem(ref ctx, gen, fields[i]);
                }
                    
                // Properties
                if ((mode & SaveMembersMode.Properties) > 0)
                {
                    var properties = ctx.ClassType.GetProperties(bindingFlags);

                    for (int i = 0; i < properties.Length; i++)
                    {
                        if (!properties[i].CanRead || !properties[i].CanWrite) continue;
                        AddItem(ref ctx, gen, properties[i]);
                    }
                }
            }

            static void AddItem(ref TranslationContext ctx, MapGenerator gen, MemberInfo info)
            {
                ObjectIntermediateItem dest = new ObjectIntermediateItem();
                dest.Details.Unprocessed = info;

                FillAttributeInfo(ref ctx, dest, info);

                ctx.CurrentMembers.Add(dest);
            }

            static SaveMembersMode GetClassMode(Type classType)
            {
                // TODO: This is just to temporarily support "object" until proper settings mapping comes in.
                if (classType == typeof(object)) return SaveMembersMode.Fields;

                var attribute = classType.GetCustomAttribute<SaveMembersAttribute>(false);
                if (attribute == null) throw new UnserializableType(classType);

                return attribute.Mode;
            }

            static void FillAttributeInfo(ref TranslationContext ctx, ObjectIntermediateItem dest, MemberInfo info)
            {
                // Get the attributes.
                var attributes = info.GetCustomAttributes(typeof(MapAttr), true);
                if (attributes.Length == 0) return;

                // Interpret the attributes.
                bool loadedSaveAttribute = false;
                for (int i = 0; i < attributes.Length; i++)
                {
                    switch (attributes[i])
                    {
                        case SaveAttribute save:
                            FillMainInfo(ref ctx, dest, save.Order, save.FromVer, save.ToVer);
                            loadedSaveAttribute = true;
                            break;
                    }
                }

                if (!loadedSaveAttribute) throw new IncompleteDetailsException(info);
            }
        }

        internal static void FillMainInfo(ref TranslationContext ctx, ObjectIntermediateItem newItem, int order, int startVer, int endVer)
        {
            newItem.Order = order;

            // Check ordering
            if (ctx.TranslationCurrentOrderInfo != -1)
            {
                if (order >= ctx.TranslationCurrentOrderInfo)
                    ctx.TranslationCurrentOrderInfo = order;
                else
                    ctx.TranslationCurrentOrderInfo = -1;
            }

            // If the version given is -1, that means it doesn't have a set end, so we'll just fill that in with "uint.MaxValue".
            // In this situation we'll only update the highest version based on what the minimum is.
            if (endVer == -1)
            {
                newItem.StartVer = checked((uint)startVer);
                newItem.EndVer = uint.MaxValue;

                if (startVer > ctx.HighestVersion)
                    ctx.HighestVersion = startVer;
            }
                

            // If not, place it in as is, and track what the highest version is based on what their custom high is.
            else
            {
                newItem.StartVer = checked((uint)startVer);
                newItem.EndVer = checked((uint)endVer);

                if (endVer > ctx.HighestVersion)
                    ctx.HighestVersion = endVer;
            }
        }

        internal struct TranslationContext
        {
            public List<ObjectIntermediateItem> CurrentMembers;
            public Type ClassType;
            public int TranslationCurrentOrderInfo;
            public int HighestVersion;

            public TranslationContext(Type classType)
            {
                ClassType = classType;
                CurrentMembers = new List<ObjectIntermediateItem>();
                TranslationCurrentOrderInfo = 0;
                HighestVersion = 0;
            }
        }
    }
}
