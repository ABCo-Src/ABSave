using ABCo.ABSave.Configuration;
using ABCo.ABSave.Converters;
using ABCo.ABSave.Mapping.Description.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

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
                    return UseConverter(GetConverterInstance(currentGenericConv), currentGenericConv.ConverterId, type);
            }
            else
            {
                if (settings.ExactConverters.TryGetValue(type, out var currentConv))
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
                        return UseConverter(converter, converterInfo.ConverterId, type);
                }
            }

            return null;
        }

        internal MapItem UseConverter(Converter converter, int id, Type type)
        {
            // Remove it from the cache.
            _converterCache[id] = null;

            ApplyItem(converter, type);
            converter.Initialize(new InitializeInfo(type, this));
            converter._allInheritanceAttributes = GetInheritanceAttributes(type);
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

        // Temporary helper until object conversion gets moved into its own converter
        internal static SaveInheritanceAttribute? GetConverterInheritanceInfoForVersion(uint version, Converter converter)
        {
            // Try to get it from the dictionary cache.
            if (converter._inheritanceValues != null && 
                converter._inheritanceValues.TryGetValue(version, out SaveInheritanceAttribute? res))
                return res;

            // If it's not in there, find it in the attribute array.
            if (converter._allInheritanceAttributes == null) return null;
            SaveInheritanceAttribute? attribute = FindInheritanceAttributeForVersion(converter._allInheritanceAttributes, version);

            converter._inheritanceValues ??= new Dictionary<uint, SaveInheritanceAttribute?>();
            converter._inheritanceValues.Add(version, attribute);
            return attribute;
        }
    }
}
