using System;

namespace ABCo.ABSave.Mapping.Description.Attributes
{
    public abstract class AttributeWithVersion : Attribute, IComparable<AttributeWithVersion>
    {
        /// <summary>
        /// The version number this attribute takes effect on.
        /// </summary>
        public uint FromVer = 0;

        /// <summary>
        /// The last version number this attribute takes effect on. This value is EXCLUSIVE. 
        /// For example, if <see cref="FromVer"/> was 1 and <see cref="ToVer"/> was 2 then this attribute would only be available in version 1, no other versions.
        /// </summary>
        public uint ToVer = uint.MaxValue;

        internal AttributeWithVersion() { }

        // Allow sorting by lower version.
        public int CompareTo(AttributeWithVersion? other)
        {
            if (FromVer < other!.FromVer)
                return 1;
            else if (FromVer > other.FromVer)
                return -1;
            else
                return 0;
        }
    }
}
