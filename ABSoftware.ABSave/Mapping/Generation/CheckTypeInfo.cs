using ABCo.ABSave.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Mapping.Generation
{
    public struct CheckTypeInfo
    {
        public Type Type { get; }
        public ABSaveSettings Settings { get; }

        internal CheckTypeInfo(Type type, ABSaveSettings settings)
        {
            Type = type;
            Settings = settings;
        }
    }
}
