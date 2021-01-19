using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Mapping.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace ABSoftware.ABSave.Helpers
{
    internal static class ABSaveUtils
    {
        internal const int MAX_STACK_SIZE = 1024; // Pushing it a little bit, but the .NET source finds 1024 an acceptable size to stack allocate so we will follow.
        internal const BindingFlags DefaultBindingFlags = BindingFlags.Public | BindingFlags.Instance;

        internal static readonly int[] IntFillMap = new int[]
        {
            0,
            0b1,
            0b11,
            0b111,
            0b1111,
            0b11111,
            0b111111,
            0b1111111,
            0b11111111
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ContainsZeroByteLong(ulong l) => ((l - 0x0101010101010101L) & ~l & 0x8080808080808080L) > 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ContainsZeroByte(uint l) => ((l - 0x01010101L) & ~l & 0x80808080L) > 0;

        internal static MapItem GetRuntimeMapItem(Type type, ABSaveMap parent)
        {
            // Try and get the map quickly.
            if (parent.CachedSubItems.TryGetValue(type, out RuntimeMapItem map)) return map;

            // Get the map slowly, and cache it.
            var itm = new RuntimeMapItem(MapGenerator.Generate(MapGenerator.GenerateItemType(type), parent));
            parent.CachedSubItems.Add(type, itm);
            return itm;
        }

        internal static T[] CreateUninitializedArray<T>(int length)
        {
            // TODO: Add .NET 5 GC.GetUnintiailizedArray support
            return new T[length];
        }
    }
}
