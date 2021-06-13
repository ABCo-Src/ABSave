using ABCo.ABSave.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Mapping.Generation
{
    public struct InitializeInfo
    {
        public Type Type { get; }
        public ABSaveSettings Settings { get; }

        MapGenerator _gen;

        public MapItemInfo GetMap(Type type) => _gen.GetMap(type);

        internal InitializeInfo(Type type, ABSaveSettings settings, MapGenerator gen)
        {
            Type = type;
            Settings = settings;
            _gen = gen;
        }
    }
}