using ABCo.ABSave.Mapping;
using System.Runtime.InteropServices;

namespace ABCo.ABSave.Helpers
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct AlreadySavedVersionInfo
    {
        [FieldOffset(0)]
        public int Converter;

        [FieldOffset(0)]
        public VersionInfo Object;
    }
}
