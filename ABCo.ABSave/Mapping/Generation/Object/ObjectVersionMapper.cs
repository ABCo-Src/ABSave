using ABCo.ABSave.Serialization.Converters;
using ABCo.ABSave.Mapping.Generation.IntermediateObject;
using System;
using System.Collections.Generic;
using System.Reflection;
using ABCo.ABSave.Mapping.Description.Attributes;
using ABCo.ABSave.Helpers;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using ABCo.ABSave.Exceptions;

namespace ABCo.ABSave.Mapping.Generation.Object
{
    internal static class ObjectVersionMapper
    {
        public static ObjectMemberSharedInfo[] GenerateNewVersion(ObjectConverter item, MapGenerator gen, uint targetVersion)
        {
            IntermediateItem[] rawMembers = item._intermediateInfo.Members!;
            bool hasMembersWithMultipleOrders = item._intermediateInfo.HasMembersWithMultipleOrders;

            var lst = new List<ObjectMemberSharedInfo>();

            // Get the members
            for (int i = 0; i < rawMembers.Length; i++)
            {
                IntermediateItem? intermediateItm = rawMembers[i];

                if (targetVersion >= intermediateItm.FromVer && targetVersion < intermediateItm.ToVer)
                {
                    if (intermediateItm.HasMultipleOrders)
                    {
                        Debug.Assert(hasMembersWithMultipleOrders);
                        InsertMultipleOrderItem(lst, intermediateItm, item, gen, targetVersion);
                    }
                    else
                        lst.Add(GetOrCreateSingleOrderItemFrom(item, intermediateItm, gen, hasMembersWithMultipleOrders));
                }
            }

            return lst.ToArray();
        }

        public static ObjectMemberSharedInfo[] GenerateForOneVersion(ObjectConverter item, MapGenerator gen)
        {
            // The only way they could physically have a member with multiple orders and still only have one version is if they've got different Save attributes sharing the same versions on that member,
            // so we'll just error out now because that's not right.
            if (item._intermediateInfo.HasMembersWithMultipleOrders)
                throw new InvalidSaveAttributeSetException(item.ItemType);

            IntermediateItem[] rawMembers = item._intermediateInfo.Members!;

            // No need to do any checks at all - just copy the items right across!
            var outputArr = new ObjectMemberSharedInfo[rawMembers.Length];

            for (int i = 0; i < outputArr.Length; i++)
                outputArr[i] = CreateItem(item, rawMembers[i], gen, rawMembers[i].SingleOrder, false);

            return outputArr;
        }

        static ObjectMemberSharedInfo CreateItem(ObjectConverter item, IntermediateItem intermediate, MapGenerator gen, int order, bool includeOrder)
        {
            ObjectMemberSharedInfo dest = includeOrder ? new ObjectMemberSharedInfoWithOrder(order) : new ObjectMemberSharedInfo();
            SetupItem(item, intermediate, gen, dest);
            return dest;
        }

        static void SetupItem(ObjectConverter item, IntermediateItem intermediate, MapGenerator gen, ObjectMemberSharedInfo dest)
        {
            MemberInfo? memberInfo = intermediate.Details.Unprocessed;

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
        }

        static ObjectMemberSharedInfo GetOrCreateSingleOrderItemFrom(ObjectConverter item, IntermediateItem intermediate, MapGenerator gen, bool hasMemberWithMultipleOrders)
        {
            if (!intermediate.IsProcessed)
            {
                lock (intermediate)
                {
                    // Now that we've taken the lock it may have been marked as processed while we waiting for it.
                    // So check one more time to ensure that isn't the case.
                    if (!intermediate.IsProcessed)
                    {
                        intermediate.Details.Processed = CreateItem(item, intermediate, gen, intermediate.SingleOrder, hasMemberWithMultipleOrders);
                        intermediate.IsProcessed = true;
                    }
                }
            }

            return intermediate.Details.Processed;
        }

        private static void InsertMultipleOrderItem(List<ObjectMemberSharedInfo> lst, IntermediateItem intermediateItm, ObjectConverter objectConverter, MapGenerator gen, uint targetVersion)
        {
            var multipleOrders = (IntermediateItemWithMultipleOrders)intermediateItm;

            // We need to find the right "Save" attribute for this version. Don't include the item if there isn't actually one in there.
            SaveAttribute? attr = MappingHelpers.FindAttributeForVersion(multipleOrders.AllOrders, targetVersion);
            if (attr == null) return;

            // Now, we need to insert this property into the right place in the list, because its order varies based on the version, so it may not be in the right place in this list.
            for (int i = lst.Count - 1; i >= 0; i--)
            {
                var currentItm = Unsafe.As<ObjectMemberSharedInfoWithOrder>(lst[i])!;

                if (attr.Order > currentItm.Order)
                {
                    InsertNewAt(i);
                    return;
                }
            }

            InsertNewAt(0);

            void InsertNewAt(int i)
            {
                // Create a new "ObjectMemberSharedInfoWithOrder". We create a new one everytime, and don't go via "GetOrCreateSingleOrderItemFrom", because the "Order" varies by the version.
                var newInfo = CreateItem(objectConverter, intermediateItm, gen, attr.Order, true);

                // Performance Note: Yes, inserting into the middle of a list is a little poor, but it's very uncommon to have multiple Save attributes on one property, so it's not a very high-priority path.
                lst.Insert(i, newInfo);
            }
        }
    }
}
