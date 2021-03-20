using ABSoftware.ABSave.Helpers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave.Mapping.Generation
{
    /// <summary>
    /// Retrieves information via reflection about all of the applicable members of a type, and loads it
    /// into an intermediary format used to make the final map.
    /// </summary>
    internal static class GenObjectReflector
    {
        static void Initialize(ref ObjectReflectorInfo dest)
        {
            dest.UnmappedMembers = 0;

            dest.Members = new LoadOnceList<ObjectReflectorItemInfo>(
                LightConcurrentPool<ObjectReflectorItemInfo[]>.TryRent() ?? new ObjectReflectorItemInfo[8]);
        }

        /// <summary>
        /// Clears the list <see cref="MapGenerator.CurrentObjMembers"/> and inserts all of the (ordered) members and information attached to them.
        /// </summary>
        internal static void GetAllMembersInfo(ref ObjectReflectorInfo dest, Type type, MapGenerator gen)
        {
            Initialize(ref dest);

            var bindingFlags = GetFlagsForAccessLevel(gen.Map.Settings.IncludePrivate);

            // Fields
            if (gen.Map.Settings.ConvertFields)
            {
                var fields = type.GetFields(bindingFlags);

                for (int i = 0; i < fields.Length; i++)
                    AddItemInfo(ref dest, gen, fields[i], fields[i].FieldType);
            }

            // Properties
            else
            {
                var properties = type.GetProperties(bindingFlags);

                for (int i = 0; i < properties.Length; i++)
                {
                    if (!properties[i].CanRead || !properties[i].CanWrite) continue;
                    AddItemInfo(ref dest, gen, properties[i], properties[i].PropertyType);
                }
            }
        }

        static void AddItemInfo(ref ObjectReflectorInfo dest, MapGenerator gen, MemberInfo info, Type memberType)
        {
            ref ObjectReflectorItemInfo currentItem = ref dest.Members.CreateAndGet();
            currentItem.Info = info;
            currentItem.NameKey = info.Name;
            currentItem.MemberType = memberType;

            // Handle mapping
            if (MapGenerator.TryGetItemFromDict(gen.Map.GenInfo.AllTypes, memberType, MapItemState.Planned, out MapItemInfo val))
                currentItem.ExistingMap = val;
            else
                dest.UnmappedMembers++;
        }

        internal static bool IsField(MapGenerator gen)
        {
            return gen.Map.Settings.ConvertFields;
        }

        public static void Release(ref ObjectReflectorInfo dest)
        {
            LightConcurrentPool<ObjectReflectorItemInfo[]>.Release(dest.Members.ReleaseBuffer());
        }

        static BindingFlags GetFlagsForAccessLevel(bool includePrivate) =>
            includePrivate ? BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance : BindingFlags.Public | BindingFlags.Instance;
    }

    internal struct ObjectReflectorInfo
    {
        public int UnmappedMembers;
        public LoadOnceList<ObjectReflectorItemInfo> Members;
    }

    internal struct ObjectReflectorItemInfo
    {
        public Type MemberType;
        public MapItemInfo? ExistingMap;
        public string NameKey;
        public MemberInfo Info;        
        public MemberAccessor Accessor;
        //public int IntKey; Coming soon
    }
}
