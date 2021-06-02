using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ABSoftware.ABSave.Mapping.Description.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class SaveAttribute : MapAttr
    {
        public int Order;

        /// <summary>
        /// The version number this member starts on.
        /// </summary>
        public int FromVer = 0;

        /// <summary>
        /// The last version number this member is available on. 
        /// For example, if <see cref="FromVer"/> was 1 and <see cref="ToVer"/> was 1 then this member would only be available in version 1, no other versions.
        /// </summary>
        public int ToVer = -1; // -1 marks that this is available up to any version.

        public SaveAttribute([CallerLineNumber]int order = 0)
            => Order = order;
    }
}
