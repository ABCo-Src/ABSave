using ABCo.ABSave.Mapping;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ABCo.ABSave.Helpers
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct AlreadySavedVersionInfo
    {
        [FieldOffset(0)]
        public int Converter;

        [FieldOffset(0)]
        public ObjectVersionInfo Object;
    }
}
