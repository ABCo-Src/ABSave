using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Mapping.Generation
{
    public struct MapRetriever
    {
        MapGenerator _gen;

        public MapItemInfo GetMap(Type type) => _gen.GetMap(type);

        public MapRetriever(MapGenerator gen) => _gen = gen;
    }
}