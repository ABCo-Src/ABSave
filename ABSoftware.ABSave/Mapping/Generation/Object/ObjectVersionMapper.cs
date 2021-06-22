using ABCo.ABSave.Converters;
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
        public static ObjectMemberSharedInfo[] GenerateNewVersion(ObjectConverter item, MapGenerator gen, uint targetVersion)
        {
            var rawMembers = item._rawMembers!;
            var lst = new List<ObjectMemberSharedInfo>();

            // Get the members
            for (int i = 0; i < rawMembers.Length; i++)
            {
                var intermediateItm = rawMembers[i];

                if (targetVersion >= intermediateItm.StartVer && targetVersion <= intermediateItm.EndVer)
                    lst.Add(GetOrCreateItemFrom(item, intermediateItm, gen));
            }

            return lst.ToArray();
        }

        public static ObjectMemberSharedInfo[] GenerateForOneVersion(ObjectConverter item, MapGenerator gen, uint version)
        {
            var rawMembers = item._rawMembers!;

            // No need to do any checks at all - just copy the items right across!
            var outputArr = new ObjectMemberSharedInfo[rawMembers.Length];

            for (int i = 0; i < outputArr.Length; i++)
                outputArr[i] = CreateItem(item, rawMembers[i], gen);

            return outputArr;
        }

        static ObjectMemberSharedInfo CreateItem(ObjectConverter item, ObjectIntermediateItem intermediate, MapGenerator gen)
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
                MemberAccessorGenerator.GeneratePropertyAccessor(gen, dest, property, item);
            }
            else throw new Exception("Unrecognized member info in shared info");

            dest.Map = gen.GetMap(itemType);
            return dest;
        }

        static ObjectMemberSharedInfo GetOrCreateItemFrom(ObjectConverter item, ObjectIntermediateItem intermediate, MapGenerator gen)
        {
            if (!intermediate.IsProcessed)
            {
                lock (intermediate)
                {
                    // Now that we've taken the lock it may have been marked as processed while we waiting for it.
                    // So check one more time to ensure that isn't the case.
                    if (!intermediate.IsProcessed)
                    {
                        intermediate.Details.Processed = CreateItem(item, intermediate, gen);
                        intermediate.IsProcessed = true;
                    }
                }
            }

            return intermediate.Details.Processed;
        }

    }
}
