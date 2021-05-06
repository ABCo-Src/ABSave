using ABSoftware.ABSave.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Mapping
{
    public struct MapItemInfo : IEquatable<MapItemInfo>
    {
        internal static MapItemInfo None => new MapItemInfo(new NonReallocatingListPos(ushort.MaxValue, 255));

        internal NonReallocatingListPos Pos; // "Flag" is used to represent "IsNullable".

        public bool IsNullable
        {
            get => Pos.Flag;
            set => Pos.Flag = value;
        }

        internal MapItemInfo(NonReallocatingListPos pos) => Pos = pos;
        internal MapItemInfo(NonReallocatingListPos pos, bool isNullable)
        {
            pos.Flag = isNullable;
            Pos = pos;
        }

        public override int GetHashCode() => Pos.GetHashCode();
        public override bool Equals(object obj) => obj is MapItemInfo info && Equals(info);
        public bool Equals(MapItemInfo other) => Pos.Equals(other.Pos);
    }

    struct GenMapItemInfo
    {
        public MapItemState State;
        public MapItemInfo Info;

        public GenMapItemInfo(MapItemState state) => (State, Info) = (state, default);
        public GenMapItemInfo(MapItemInfo info) => (State, Info) = (MapItemState.Ready, info);
    }

    enum MapItemState : byte
    {
        Ready,
        Allocating,
        Planned
    }
}
