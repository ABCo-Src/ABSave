using ABSoftware.ABSave.Helpers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ABSoftware.ABSave.Mapping.Generation
{
    internal class IntermediateObjInfo
    {
        public int UnmappedCount;
        public int HighestVersion;

        public Type ClassType = null!;
        public ObjectTranslatedItemInfo[] RawMembers = null!;

        // Null if "RawMembers" is already sorted.
        public ObjectTranslatedSortInfo[]? SortedMembers;

        public void Initialize(Type classType)
            => ClassType = classType;

        public ref struct MemberIterator
        {
            readonly IntermediateObjInfo _info;

            public int Length => _info.RawMembers.Length;
            public int Index;

            public MemberIterator(IntermediateObjInfo info) : this()
            {
                Index = 0;
                _info = info;
            }

            public bool MoveNext() => ++Index < Length;

            public ref ObjectTranslatedItemInfo GetCurrent()
            {
                if (_info.SortedMembers == null)
                    return ref _info.RawMembers[Index];
                else
                    return ref _info.RawMembers[_info.SortedMembers[Index].Index];
            }
        }
    }

    [StructLayout(LayoutKind.Auto)]
    internal struct ObjectTranslatedItemInfo
    {
        public Type MemberType;
        public int Order;

        public uint StartVer;
        public uint EndVer;

        public MapItemInfo? ExistingMap;

        public MemberInfo Info;
        public MemberAccessor Accessor;
    }

    internal struct ObjectTranslatedSortInfo : IComparable<ObjectTranslatedSortInfo>
    {
        public int Order;
        public short Index;

        public int CompareTo(ObjectTranslatedSortInfo other) => Order.CompareTo(other.Order);
    }
}
