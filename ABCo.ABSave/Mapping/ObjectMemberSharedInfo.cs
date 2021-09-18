using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Mapping
{
    /// <summary>
    /// Info about a member that's shared across all versions it occurs.
    /// </summary>
    internal class ObjectMemberSharedInfo
    {
        public Converter Map;
        public MemberAccessor Accessor;
    }
}
