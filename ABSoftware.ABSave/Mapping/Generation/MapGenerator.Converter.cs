using ABCo.ABSave.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Mapping.Generation
{
    public partial class MapGenerator
    {
        internal MapItem? TryGenerateConverter(Type type)
        {
            var genContext = new ContextGen(type, this);

            if (!TryGetConverter(ref genContext))
                return null;

            return genContext.ContextInstance;
        }

        static bool TryGetConverter(ref ContextGen gen)
        {
            var settings = gen.Settings;

            // Exact converter
            if (settings.ExactConverters != null && settings.ExactConverters.TryGetValue(gen.Type, out var currentConverter))
            {
                currentConverter.TryGenerateContext(ref gen);

                if (gen.ContextInstance == null)
                    throw new Exception("Converter failed to provide a context for one of its exact (and by extension guaranteed) types.");

                goto Found;
            }

            // Non-exact converter
            if (settings.NonExactConverters != null)
            {
                for (int i = settings.NonExactConverters.Count - 1; i >= 0; i--)
                {
                    currentConverter = settings.NonExactConverters[i];

                    currentConverter.TryGenerateContext(ref gen);

                    if (gen.ContextInstance != null)
                        goto Found;
                }
            }

            return false;

        Found:
            gen.ContextInstance._converter = currentConverter;
            return true;
        }
    }

    public struct ContextGen
    {
        public Type Type;
        internal ConverterContext? ContextInstance; // Null if not assigned by the user
        readonly MapGenerator _gen;

        public ABSaveMap Map => _gen.Map;
        public ABSaveSettings Settings => _gen.Map.Settings;

        /// <summary>
        /// Once it has been decided the converter will convert the type, this MUST be used to provide what context will be used. 
        /// The context may be left null in which case an instance will be filled in and provided at serialization-time with all the correct basic details.
        /// </summary>
        public void AssignContext(ConverterContext? contextInstance, uint maximumVersion)
        {
            ContextInstance = contextInstance;
            ContextInstance ??= new ConverterContext();
            _gen.ApplyItem(ContextInstance, Type);
            ContextInstance.HighestVersion = maximumVersion;
        }

        /// <summary>
        /// Gets the map for the given type.
        /// </summary>
        public MapItemInfo GetMap(Type type)
        {
            // Force converters to "AssignContext" first to get the map item out of the allocating state as quickly as possible.
            if (ContextInstance == null)
                throw new Exception("Converter must assign its context first before attempting to get other maps.");

            return _gen.GetMap(type);
        }

        internal ContextGen(Type type, MapGenerator gen) =>
            (Type, _gen, ContextInstance) = (type, gen, null);
    }
}
