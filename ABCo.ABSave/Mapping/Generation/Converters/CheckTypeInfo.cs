using ABCo.ABSave.Configuration;
using System;

namespace ABCo.ABSave.Mapping.Generation.Converters
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
