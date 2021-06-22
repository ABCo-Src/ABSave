using ABCo.ABSave.Mapping.Description.Attributes;
using ABCo.ABSave.Mapping.Generation.Inheritance;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ABCo.ABSave.Mapping.Generation.Object
{
    internal static class ObjectVersionMapper
    {
        public static VersionInfo GenerateNewVersion(MapGenerator gen, uint targetVersion, ObjectMapItem parent)
        {
            var intermediate = parent.Intermediate;
            var lst = new List<ObjectMemberSharedInfo>();

            // Get the members
            for (int i = 0; i < intermediate.RawMembers.Length; i++)
            {
                var intermediateItm = intermediate.RawMembers[i];

                if (targetVersion >= intermediateItm.StartVer && targetVersion <= intermediateItm.EndVer)
                    lst.Add(GetOrCreateItemFrom(intermediateItm, parent, gen));
            }

            var inheritanceInfo = InheritanceHandler.GetAttributeForVersion(intermediate.AllInheritanceAttributes, targetVersion);
            return new VersionInfo(lst.ToArray(), inheritanceInfo);
        }

        public static VersionInfo GenerateForOneVersion(MapGenerator gen, uint version, ObjectMapItem parent)
        {
            var intermediate = parent.Intermediate;

            // No need to do any checks at all - just copy the items right across!
            var outputArr = new ObjectMemberSharedInfo[intermediate.RawMembers.Length];

            for (int i = 0; i < outputArr.Length; i++)
                outputArr[i] = CreateItem(parent, intermediate.RawMembers[i], gen);

            SaveInheritanceAttribute? inheritanceInfo = InheritanceHandler.GetAttributeForVersion(intermediate.AllInheritanceAttributes, version);
            return new VersionInfo(outputArr, inheritanceInfo);
        }

        static ObjectMemberSharedInfo CreateItem(ObjectMapItem parent, ObjectIntermediateItem intermediate, MapGenerator gen)
        {
            var dest = new ObjectMemberSharedInfo();
            var memberInfo = intermediate.Details.Unprocessed;

            Type itemType;

            if (memberInfo is FieldInfo field)
            {
                itemType = field.FieldType;
                MemberAccessorGenerator.GenerateFieldAccessor(ref dest.Accessor, memberInfo);
            }
            else if (memberInfo is PropertyInfo property)
            {
                itemType = property.PropertyType;
                MemberAccessorGenerator.GeneratePropertyAccessor(gen, dest, property, parent);
            }
            else throw new Exception("Unrecognized member info in shared info");

            dest.Map = gen.GetMap(itemType);
            return dest;
        }

        static ObjectMemberSharedInfo GetOrCreateItemFrom(ObjectIntermediateItem intermediate, ObjectMapItem parent, MapGenerator gen)
        {
            if (!intermediate.IsProcessed)
            {
                lock (intermediate)
                {
                    // Now that we've taken the lock it may have been marked as processed while we waiting for it.
                    // So check one more time to ensure that isn't the case.
                    if (!intermediate.IsProcessed)
                    {
                        intermediate.Details.Processed = CreateItem(parent, intermediate, gen);
                        intermediate.IsProcessed = true;
                    }
                }
            }

            return intermediate.Details.Processed;
        }

    }
}
