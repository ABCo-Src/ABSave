using ABSoftware.ABSave.Exceptions;
using ABSoftware.ABSave.Mapping.Description;
using ABSoftware.ABSave.Mapping.Description.Attributes;
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

        internal static ObjectVersionInfo GetVersionOrAddNull(uint version, ObjectMapItem parentObj)
        {
            if (parentObj.HasOneVersion)
                return version > 0 ? ObjectVersionInfo.None : parentObj.Members.OneVersion;

            while (true)
            {
                lock (parentObj.Members.MultipleVersions)
                {
                    // Does not exist - Has not and is not generating.
                    // Exists but is null - Is currently generating.
                    // Exists and is not null - Is ready to use.
                    if (parentObj.Members.MultipleVersions.TryGetValue(version, out ObjectVersionInfo info))
                    {
                        if (info.Members != null) return info;
                    }
                    else
                    {
                        parentObj.Members.MultipleVersions.Add(version, ObjectVersionInfo.None);
                        return ObjectVersionInfo.None;
                    }
                }

                Thread.Yield();
            }
        }

        internal ObjectVersionInfo AddNewVersion(uint version, ObjectMapItem parentObj)
        {
            if (parentObj.HasOneVersion || version > parentObj.HighestVersion)
                throw new UnsupportedVersionException(parentObj.ItemType, version);

            var newVer = GenerateNewVersion(version, parentObj);

            lock (parentObj.Members.MultipleVersions)
            {
                parentObj.Members.MultipleVersions[version] = newVer;
                if (parentObj.Members.MultipleVersions.Count > parentObj.HighestVersion)
                    parentObj.Intermediate.RawMembers = null!;
            }

            return newVer;
        }

        void FillDestWithNoMembers(ObjectMapItem dest)
        {
            dest.Intermediate.Release();

            dest.HasOneVersion = true;
            dest.HighestVersion = 0;
            dest.Members.OneVersion = new ObjectVersionInfo(Array.Empty<ObjectMemberSharedInfo>(), null);
        }

        void FillDestWithOneVersion(ObjectMapItem dest)
        {
            dest.HasOneVersion = true;
            dest.Members.OneVersion = GenerateForOneVersion(dest.HighestVersion, dest);

            // There are no more versions here, drop the raw members.
            dest.Intermediate.Release();
        }

        void FillDestWithMultipleVersions(ObjectMapItem dest)
        {
            dest.HasOneVersion = false;
            dest.Members.MultipleVersions = new Dictionary<uint, ObjectVersionInfo>();
        }

        internal MapItem GenerateNewObject(Type type)
        {
            var res = new ObjectMapItem();
            ApplyItem(res, type);

            res.HighestVersion = CreateIntermediateObjectInfo(type, ref res.Intermediate);

            if (res.Intermediate.RawMembers!.Length == 0)
                FillDestWithNoMembers(res);
            else if (res.HighestVersion == 0)
                FillDestWithOneVersion(res);
            else
                FillDestWithMultipleVersions(res);

            return res;
        }

        ObjectVersionInfo GenerateNewVersion(uint targetVersion, ObjectMapItem parent)
        {
            var intermediate = parent.Intermediate;
            var lst = new List<ObjectMemberSharedInfo>();

            // Get the members
            for (int i = 0; i < intermediate.RawMembers.Length; i++)
            {
                var intermediateItm = intermediate.RawMembers[i];

                if (targetVersion >= intermediateItm.StartVer && targetVersion <= intermediateItm.EndVer)
                    lst.Add(GetOrCreateItemFrom(intermediateItm, parent));
            }

            var inheritanceInfo = FindInheritanceAttributeForVersion(intermediate.AllInheritanceAttributes, targetVersion);
            return new ObjectVersionInfo(lst.ToArray(), inheritanceInfo);
        }

        ObjectVersionInfo GenerateForOneVersion(uint version, ObjectMapItem parent)
        {
            var intermediate = parent.Intermediate;

            // No need to do any checks at all - just copy the items right across!
            var outputArr = new ObjectMemberSharedInfo[intermediate.RawMembers.Length];

            for (int i = 0; i < outputArr.Length; i++)
                outputArr[i] = CreateItem(parent, intermediate.RawMembers[i]);

            SaveInheritanceAttribute? inheritanceInfo = FindInheritanceAttributeForVersion(intermediate.AllInheritanceAttributes, version);
            return new ObjectVersionInfo(outputArr, inheritanceInfo);
        }

        private static SaveInheritanceAttribute? FindInheritanceAttributeForVersion(SaveInheritanceAttribute[]? attributes, uint version)
        {
            if (attributes == null) return null;

            for (int i = 0; i < attributes.Length; i++)
            {
                var currentAttribute = attributes[i];
                if (currentAttribute.FromVer <= version && currentAttribute.ToVer >= version)
                    return currentAttribute;
            }

            return null;
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
