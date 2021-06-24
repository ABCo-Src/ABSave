using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Mapping.Description
{
    public enum SaveMembersMode : byte
    {
        Fields = 1,
        Properties = 2
    }

    public enum SaveInheritanceMode
    {
        /// <summary>
        /// Tells ABSave to use the index the type appears in the list provided to store what sub-type is present.
        /// </summary>
        Index,

        /// <summary>
        /// Tells ABSave to use a named key (provided as an attribute on each sub-type) to store what sub-type is present.
        /// The ABSave document has a built-in caching feature so the key is only ever stored once if used.
        /// You still need to provide a list of all the types with this key. You may add them via settings too
        /// </summary>
        Key,

        /// <summary>
        /// When this attribute is applied you must provide two arrays:  One for <see cref="SaveInheritanceMode.Index"/> mode 
        /// and one for <see cref="SaveInheritanceMode.Key"/> mode.
        /// </summary>
        IndexOrKey,
    }
}
