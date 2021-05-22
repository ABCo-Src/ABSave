using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping.Generation;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ABSoftware.ABSave.Mapping
{
    public struct MapItemInfo
    {
        public bool IsNullable { get; internal set; }
        internal MapItem _innerItem;

        /// <summary>
        /// Gets the item type this info represents, including the nullability.
        /// </summary>
        public Type GetItemType() =>
            IsNullable ? typeof(Nullable<>).MakeGenericType(_innerItem.ItemType) : _innerItem.ItemType;

        public bool IsValueTypeItem => IsNullable || _innerItem.IsValueItemType;

        public override bool Equals(object? obj) => obj is MapItemInfo info && this == info;
        public static bool operator ==(MapItemInfo left, MapItemInfo right) =>
            left.IsNullable == right.IsNullable && left._innerItem == right._innerItem;

        public static bool operator !=(MapItemInfo left, MapItemInfo right) =>
           left.IsNullable != right.IsNullable || left._innerItem != right._innerItem;

        public override int GetHashCode() => base.GetHashCode();

        internal MapItemInfo(MapItem item, bool isNullable) => (_innerItem, IsNullable) = (item, isNullable);
    }

    public abstract class MapItem
    {
        public Type ItemType = null!;
        public bool IsValueItemType;

        internal volatile bool IsGenerating;
        internal bool ObjectHasOneVersion;
        public int ObjectHighestVersion;
    }

    internal sealed class ObjectMapItem : MapItem
    {
        public ObjectMembers Members;
        public ObjectIntermediateItem[]? RawMembers;

        [StructLayout(LayoutKind.Explicit)]
        public struct ObjectMembers
        {
            [FieldOffset(0)]
            public ObjectMemberSharedInfo[] OneVersion;
             
            [FieldOffset(0)]
            public Dictionary<int, ObjectMemberSharedInfo[]?> MultipleVersions;
        }
    }

    /// <summary>
    /// Represents a map item that was retrieved during serialization-time. It has extra code-gen information as map items
    /// retrieved at serialization-time won't have been code-generated as a part of the main type.
    /// </summary>
    internal sealed class RuntimeMapItem : MapItem
    {
        internal MapItem InnerItem;

        public RuntimeMapItem(MapItem innerItem) => InnerItem = innerItem;

        // TODO: Add code-gen details here.
    }

    /// <summary>
    /// Info about a member that's shared across all versions it occurs.
    /// </summary>
    internal class ObjectMemberSharedInfo
    {
        public MapItemInfo Map;
        public MemberAccessor Accessor;
    }
}