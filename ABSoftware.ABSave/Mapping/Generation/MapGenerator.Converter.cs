using ABCo.ABSave.Configuration;
using ABCo.ABSave.Converters;
using ABCo.ABSave.Mapping.Generation.General;
using ABCo.ABSave.Mapping.Generation.Inheritance;
using System;

namespace ABCo.ABSave.Mapping.Generation
{
    public partial class MapGenerator
    {
        internal MapItem? TryGenerateConverter(Type type)
        {
            var settings = Map.Settings;

            if (type.IsGenericType)
            {
                var genericType = type.GetGenericTypeDefinition();
                if (settings.ExactConverters.TryGetValue(genericType, out var currentGenericConv))
                {
                    return UseConverter(GetConverterInstance(currentGenericConv), currentGenericConv.ConverterId, type);
                }
            }

            if (settings.ExactConverters.TryGetValue(type, out var currentConv))
            {
                return UseConverter(GetConverterInstance(currentConv), currentConv.ConverterId, type);
            }

            // Non-exact converter
            if (settings.NonExactConverters != null)
            {
                for (int i = settings.NonExactConverters.Count - 1; i >= 0; i--)
                {
                    var converterInfo = settings.NonExactConverters[i];
                    var converter = GetConverterInstance(converterInfo);

                    if (converter.CheckType(new CheckTypeInfo(type, settings)))
                    {
                        return UseConverter(converter, converterInfo.ConverterId, type);
                    }
                }
            }

            return null;
        }

        internal MapItem UseConverter(Converter converter, int id, Type type)
        {
            // Remove it from the cache.
            _converterCache[id] = null;

            // Apply the converter
            ApplyItem(converter, type);

            // Call the user initialization
            converter.Initialize(new InitializeInfo(type, this));

            // Setup the backing information for the converter.
            converter._allInheritanceAttributes =
                InheritanceHandler.GetInheritanceAttributes(type, ref converter.HighestVersion);

            VersionCacheHandler.SetupVersionCacheOnItem(converter, this);
            return converter;
        }

        // CONVERTER CREATION:
        // In order for us to use "CheckType" when we check whether a converter will convert a given type, we need
        // to actually have an instance of that converter in the first place.
        //
        // The simple way to do that would be to simply make a new instance of a converter every single time
        // we want to run "CheckType".
        //
        // However, that means we're creating potentially A LOT of converters that will never be used, just to run
        // "CheckType". So, instead of doing that, we're going to cache instances in here. The first time we go to
        // "CheckType" on a converter, we create an instance in here.
        //
        // And if the "CheckType" fails, we will keep that instance in the cache for the next time we want to check.
        //
        // And if the "CheckType" succeeds, that means we'll be using that instance in the final map, and we'll therefore
        // remove it from the cache for a new one to be created the next time "CheckType" in executed.
        //
        // (The indicies in this array line up to the ID of a converter. So index 1 will be the cache for the converter
        // with an ID of 1)
        Converter?[] _converterCache = null!;

        Converter GetConverterInstance(ConverterInfo info) =>
            _converterCache[info.ConverterId] ??= (Converter)Activator.CreateInstance(info.ConverterType)!;
    }
}
