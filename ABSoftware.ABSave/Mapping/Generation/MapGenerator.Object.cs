using ABSoftware.ABSave.Exceptions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ABSoftware.ABSave.Mapping.Generation
{
    // Creates a final map of an object using intermediate information from the "IntermediateObject" part.
    public partial class MapGenerator
    {
        struct Context
        {
            public uint TargetVersion;
            public ObjectMapItem Dest;

            public Context(uint targetVersion, ObjectMapItem dest)
                => (TargetVersion, Dest) = (targetVersion, dest);
        }

        internal static ObjectMemberSharedInfo[]? GetVersionOrAddNull(uint version, ObjectMapItem parentObj)
        {
            if (parentObj.ObjectHasOneVersion)
                return version > 0 ? null : parentObj.Members.OneVersion;

            while (true)
            {
                lock (parentObj.Members.MultipleVersions)
                {
                    // Does not exist - Has not and is not generating.
                    // Exists but is null - Is currently generating.
                    // Exists and is not null - Is ready to use.
                    if (parentObj.Members.MultipleVersions.TryGetValue(version, out ObjectMemberSharedInfo[]? info))
                    {
                        if (info != null) return info;
                    }
                    else
                    {
                        parentObj.Members.MultipleVersions.Add(version, null);
                        return null;
                    }
                }

                Thread.Yield();
            }
        }

        internal ObjectMemberSharedInfo[] GenerateVersion(uint version, ObjectMapItem parentObj)
        {
            uint latestVer = parentObj.ObjectHighestVersion;

            if (parentObj.ObjectHasOneVersion || version > latestVer)
                throw new UnsupportedVersionException(parentObj.ItemType, version);

            var ctx = new Context(version, parentObj);
            var newVer = GenerateNewVersion(ref ctx, parentObj);

            lock (parentObj.Members.MultipleVersions)
            {
                parentObj.Members.MultipleVersions[version] = newVer;
                if (parentObj.Members.MultipleVersions.Count > parentObj.ObjectHighestVersion)
                    parentObj.RawMembers = null;
            }

            return newVer;
        }

        internal MapItem GenerateNewObject(Type type)
            => GenerateNewObject(type, CreateIntermediateObjectInfo(type));

        internal MapItem GenerateNewObject(Type type, IntermediateObjInfo info)
        {
            var res = new ObjectMapItem();
            ApplyItem(res, type);

            // If there's literally zero members, just do nothing.
            if (info.RawMembers.Length == 0)
                return GenerateNoMembers(res);

            res.RawMembers = info.RawMembers;
            res.ObjectHighestVersion = info.HighestVersion;

            // Handle objects with only one version quickly without any extra work.
            if (info.HighestVersion == 0)
            {
                var ctx = new Context(0, res);
                GenerateForOneVersion(ref ctx, res);

                // There are no more versions here, drop the raw members.
                res.RawMembers = null;
            }
            else
            {
                res.ObjectHasOneVersion = false;
                res.RawMembers = info.RawMembers;
                res.Members.MultipleVersions = new Dictionary<uint, ObjectMemberSharedInfo[]?>();
            }

            return res;
        }

        static MapItem GenerateNoMembers(ObjectMapItem res)
        {
            res.RawMembers = null;
            //IntermediateObjInfoMapper.Release(info);

            res.ObjectHasOneVersion = true;
            res.ObjectHighestVersion = 0;
            res.Members.OneVersion = Array.Empty<ObjectMemberSharedInfo>();
            return res;
        }

        ObjectMemberSharedInfo[] GenerateNewVersion(ref Context ctx, ObjectMapItem parent)
        {
            var lst = new List<ObjectMemberSharedInfo>();
            uint version = ctx.TargetVersion;

            for (int i = 0; i < ctx.Dest.RawMembers!.Length; i++)
            {
                var intermediateItm = ctx.Dest.RawMembers[i];

                if (version >= intermediateItm.StartVer && version <= intermediateItm.EndVer)
                    lst.Add(GetOrCreateItemFrom(intermediateItm, parent));
            }

            return lst.ToArray();
        }

        ObjectMemberSharedInfo[] GenerateForOneVersion(ref Context ctx, ObjectMapItem dest)
        {
            // No need to do any checks at all - just copy the items right across!
            var outputArr = new ObjectMemberSharedInfo[dest.RawMembers!.Length];

            for (int i = 0; i < outputArr.Length; i++)
                outputArr[i] = CreateItem(dest, ctx.Dest.RawMembers![i]);

            dest.ObjectHasOneVersion = true;
            return dest.Members.OneVersion = outputArr;
        }

        ObjectMemberSharedInfo GetOrCreateItemFrom(ObjectIntermediateItem intermediate, ObjectMapItem parent)
        {
            if (!intermediate.IsProcessed)
            {
                lock (intermediate)
                {
                    // Now that we've taken the lock it may have been marked as processed while we waiting for it.
                    // So check one more time to ensure that isn't the case.
                    if (!intermediate.IsProcessed)
                    {
                        intermediate.Details.Processed = CreateItem(parent, intermediate);
                        intermediate.IsProcessed = true;
                    }
                }
            }

            return intermediate.Details.Processed;
        }

        ObjectMemberSharedInfo CreateItem(ObjectMapItem parent, ObjectIntermediateItem intermediate)
        {
            var dest = new ObjectMemberSharedInfo();
            var memberInfo = intermediate.Details.Unprocessed;

            Type itemType;

            if (memberInfo is FieldInfo field)
            {
                itemType = field.FieldType;
                GenerateFieldAccessor(ref dest.Accessor, memberInfo);
            }
            else if (memberInfo is PropertyInfo property)
            {
                itemType = property.PropertyType;
                _propertyAccessorsToProcess.Add(new PropertyAccessorsToProcess(dest, parent, property));
            }
            else throw new Exception("Unrecognized member info in shared info");

            dest.Map = GetMap(itemType);
            return dest;
        }

        // Internal for testing:
        internal static void GenerateFieldAccessor(ref MemberAccessor dest, MemberInfo memberInfo) =>
            dest.Initialize(MemberAccessorType.Field, memberInfo, null);        

        /// <summary>
        /// Generate the fastest possible accessor for the given property. See more details on <see cref="MemberAccessor"/>
        /// </summary>
        internal static void GeneratePropertyAccessor(ref MemberAccessor accessor, MapItemInfo item, MapItem parentItem, PropertyInfo property)
        {
            // All property optimizations rely on the parent being a reference-type.
            if (!parentItem.IsValueItemType)
            {
                // Value type property - Just support some basic primitives.
                if (item.IsValueTypeItem)
                {
                    // Rationale: Generating an accessor for value types is very difficult, as JIT gen
                    // for value types varies wildly and is too implementation-specific. Since this 
                    // is only designed to be a quick and dirty fast accessor anyway, it's easiest if
                    // we only support the very basic primitives (which are very common anyway)
                    // and nothing more. "MakeGenericMethod" is too expensive.
                    var successful = TryGenerateAccessorPrimitive(ref accessor, parentItem.ItemType, item.GetItemType(), property);
                    if (successful) return;
                }

                // Reference type property - Simply force cast to "object".
                else
                {
                    CreateAllRefTypeAccessor(ref accessor, parentItem.ItemType, item.GetItemType(), property);
                    return;
                }

            }

            // Unoptimized
            accessor.Initialize(MemberAccessorType.SlowProperty, property, null);
        }

        internal delegate object ReferenceGetterDelegate<T>(T val);
        static readonly Type GenericRefPropertyGetter = typeof(ReferenceGetterDelegate<>);
        static readonly Type GenericPrimitivePropertyGetter = typeof(Func<,>);
        static readonly Type GenericPropertySetter = typeof(Action<,>);

        // Try to generate an accessor for a built-in primitive type.
        static bool TryGenerateAccessorPrimitive(ref MemberAccessor accessor, Type parentType, Type type, PropertyInfo property)
        {
            TypeCode typeCode = Type.GetTypeCode(type);

            // If it's not in the range of supported types (bool, DateTime, primitives) don't do the primitive accessor.
            if (typeCode < TypeCode.Boolean || typeCode > TypeCode.DateTime) return false;

            var getter = property.GetGetMethod()!.CreateDelegate(GenericPrimitivePropertyGetter.MakeGenericType(parentType, type));
            var setter = property.GetSetMethod()!.CreateDelegate(GenericPropertySetter.MakeGenericType(parentType, type));

            accessor.Initialize(MemberAccessorType.PrimitiveProperty, getter, setter);
            accessor.PrimitiveTypeCode = typeCode;
            return true;
        }

        private static void CreateAllRefTypeAccessor(ref MemberAccessor accessor, Type parent, Type itemType, PropertyInfo property)
        {
            var propGetter = property.GetGetMethod()!.CreateDelegate(
                GenericRefPropertyGetter.MakeGenericType(parent));
            
            var propSetter = property.GetSetMethod()!.CreateDelegate(
                GenericPropertySetter.MakeGenericType(parent, itemType));

            accessor.Initialize(MemberAccessorType.AllRefProperty, propGetter, propSetter);
        }

        struct PropertyAccessorsToProcess
        {
            public ObjectMemberSharedInfo Info;
            public PropertyInfo Property;
            public MapItem Parent;

            public PropertyAccessorsToProcess(ObjectMemberSharedInfo info, MapItem parent, PropertyInfo property) =>
                (Info, Parent, Property) = (info, parent, property);
        }

        // A list of all the property members to still have their accessor processed. These get
        // parallel processed at the very end of the generation process.
        List<PropertyAccessorsToProcess> _propertyAccessorsToProcess = new List<PropertyAccessorsToProcess>();

        private void ProcessAllQueuedAccessors()
        {
            Parallel.ForEach(_propertyAccessorsToProcess, s =>
            {
                GeneratePropertyAccessor(ref s.Info.Accessor, s.Info.Map, s.Parent, s.Property);
            });

            _propertyAccessorsToProcess.Clear();
        }
    }
}
