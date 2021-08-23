using ABCo.ABSave.Serialization.Converters;
using ABCo.ABSave.Mapping.Description.Attributes;
using System;

namespace ABCo.ABSave.Mapping
{
    public struct MapItemInfo
    {
        public bool IsNullable { get; internal set; }
        public Converter Converter;

        /// <summary>
        /// Gets the item type this info represents, including the nullability.
        /// </summary>
        public Type GetItemType() =>
            IsNullable ? typeof(Nullable<>).MakeGenericType(Converter.ItemType) : Converter.ItemType;

        public bool IsValueTypeItem => IsNullable || Converter.IsValueItemType;

        public override bool Equals(object? obj) => obj is MapItemInfo info && this == info;
        public static bool operator ==(MapItemInfo left, MapItemInfo right) =>
            left.IsNullable == right.IsNullable && left.Converter == right.Converter;

        public static bool operator !=(MapItemInfo left, MapItemInfo right) =>
           left.IsNullable != right.IsNullable || left.Converter != right.Converter;

        public override int GetHashCode() => base.GetHashCode();

        internal MapItemInfo(Converter item, bool isNullable) => (Converter, IsNullable) = (item, isNullable);
    }
}