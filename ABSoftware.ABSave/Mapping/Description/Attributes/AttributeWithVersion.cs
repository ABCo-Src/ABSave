using System;

namespace ABCo.ABSave.Mapping.Description.Attributes
{
    public abstract class AttributeWithVersion : Attribute 
    {
        /// <summary>
        /// The version number this attribute takes effect on.
        /// </summary>
        public uint FromVer = 0;

        /// <summary>
        /// The last version number this attribute takes effect on. 
        /// For example, if <see cref="FromVer"/> was 1 and <see cref="ToVer"/> was 1 then this attribute would only be available in version 1, no other versions.
        /// </summary>
        public uint ToVer = uint.MaxValue;
    }
}
