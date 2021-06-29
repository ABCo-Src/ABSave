using ABCo.ABSave.Mapping;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

// GC API:
#if NET5_0_OR_GREATER
using System;
#endif

namespace ABCo.ABSave.Helpers
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

        internal static bool ContainsZeroByteLong(ulong l) => ((l - 0x0101010101010101L) & ~l & 0x8080808080808080L) > 0;

        internal static bool ContainsZeroByte(uint l) => ((l - 0x01010101L) & ~l & 0x80808080L) > 0;

        internal static void WaitUntilNotGenerating(MapItem item)
        {
            if (item._isGenerating)
            {
                var waiter = new SpinWait();
                while (item._isGenerating) waiter.SpinOnce();
            }
        }

        // This will almost definitely be inlined anyway, but we may as well specifically mark it to
        // as it is VERY important that it does, so as to elide the possible generic overhead of "new T",
        // (if "T" is a reference type and won't get its own JIT instantiation anyway)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T[] CreateUninitializedArray<T>(int length) =>
            // TODO: Add .NET 5 GC.GetUnintiailizedArray support
#if NET5_0_OR_GREATER
            GC.AllocateUninitializedArray<T>(length);
#else
            new T[length];
#endif

    }
}
