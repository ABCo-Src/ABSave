using ABSoftware.ABSave.Mapping.Items;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ABSoftware.ABSave.Mapping
{
    public class ABSaveMap
    {
        internal MapItem RootItem;
        internal MapItem AssemblyItem;

        /// <summary>
        /// The configuration this map uses.
        /// </summary>
        public ABSaveSettings Settings { get; set; }

        /// <summary>
        /// Stores all sub-types that may be found inside this map and its items. These are found at serialization-time when there is inheritance type differences.
        /// </summary>
        internal Dictionary<Type, RuntimeMapItem> CachedSubItems { get; set; } = new Dictionary<Type, RuntimeMapItem>();

        // Internal for use with unit tests.
        internal ABSaveMap(ABSaveSettings settings)
        {
            Settings = settings;
            AssemblyItem = MapGenerator.Generate(typeof(Assembly), this);
        }

        public static ABSaveMap Get<T>(ABSaveSettings settings) => GetNonGeneric(typeof(T), settings);

        public static ABSaveMap GetNonGeneric(Type type, ABSaveSettings settings)
        {
            var map = new ABSaveMap(settings);
            map.RootItem = MapGenerator.Generate(type, map);
            return map;
        }

        public MapItem GetMaptimeSubItem(Type type) => MapGenerator.Generate(type, this);
    }
}
