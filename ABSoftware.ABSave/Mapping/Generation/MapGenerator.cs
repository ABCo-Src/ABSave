using ABCo.ABSave.Configuration;
using ABCo.ABSave.Converters;
using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Mapping.Generation.General;
using ABCo.ABSave.Mapping.Generation.Inheritance;
using ABCo.ABSave.Mapping.Generation.Object;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ABCo.ABSave.Mapping.Generation
{
    public partial class MapGenerator
    {
        internal ABSaveMap _map = null!;

        // A list of all the property members to still have their accessor processed. These get
        // parallel processed at the very end of the generation process.
        readonly List<MemberAccessorGenerator.PropertyToProcess> _propertyAccessorsToProcess = new List<MemberAccessorGenerator.PropertyToProcess>();

        public MapItemInfo GetMap(Type type)
        {
            bool isNullable = TryExpandNullable(ref type);

            Converter? existingItem = GetExistingOrAddNull(type);
            if (existingItem != null) return new MapItemInfo(existingItem, isNullable);

            return GenerateMap(type, isNullable);
        }

        MapItemInfo GenerateMap(Type type, bool isNullable)
        {
            Converter? item = TryGenerateConverter(type);
            if (item == null) throw new UnserializableTypeException(type);

            item._isGenerating = false;
            return new MapItemInfo(item, isNullable);
        }

        internal Converter? TryGenerateConverter(Type type)
        {
            ABSaveSettings? settings = _map.Settings;

            if (type.IsGenericType)
            {
                Type? genericType = type.GetGenericTypeDefinition();
                if (settings.ExactConverters.TryGetValue(genericType, out ConverterInfo? currentGenericConv))
                    return UseConverter(GetConverterInstance(currentGenericConv), currentGenericConv.ConverterId, type);
            }

            if (settings.ExactConverters.TryGetValue(type, out ConverterInfo? currentConv))
                return UseConverter(GetConverterInstance(currentConv), currentConv.ConverterId, type);

            // Non-exact converter
            if (settings.NonExactConverters != null)
            {
                for (int i = settings.NonExactConverters.Count - 1; i >= 0; i--)
                {
                    ConverterInfo? converterInfo = settings.NonExactConverters[i];
                    Converter? converter = GetConverterInstance(converterInfo);

                    if (converter.CheckType(new CheckTypeInfo(type, settings)))
                        return UseConverter(converter, converterInfo.ConverterId, type);
                }
            }

            return null;
        }

        internal Converter UseConverter(Converter converter, int id, Type type)
        {
            // Remove it from the cache.
            _converterCache[id] = null;

            // Apply the converter
            ApplyItem(converter, type);

            // Call the user initialization
            converter._highestVersion = converter.Initialize(new InitializeInfo(type, this));

            // Setup the backing information for the converter.
            converter._allInheritanceAttributes =
                InheritanceHandler.GetInheritanceAttributes(type, ref converter._highestVersion);

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

        internal MapItemInfo GetRuntimeMap(Type type) => GetMap(type);

        // ABSave Concurrent Generation System:
        //
        // The way this system works is when a map item is currently being generated, or is already generated,
        // it will get added to "AllTypes". When added to "AllTypes", it's given a state, these are all the
        // scearios and the states they get assigned.
        //
        // READY:
        // ------
        // The type has been fully generated.
        // 
        // READY (but currently generating):
        // ------
        // If an object is currently in the middle of being generated, the final instance will be put in "AllTypes" with
        // as "currently generating". In this situation we'll just take the map item as we are able to use items
        // while they're being generated, provided they've been allocated a place already.
        //
        // This is represented by "IsGenerating" being set on the instance, which is checked at serialization-time.
        //
        // ALLOCATING:
        // -----------
        // If an object is ABOUT to start generating, but just hasn't quite been allocated a place yet (meaning it
        // hasn't determined whether it's an object or converter yet, and as such doesn't know what to make an instance of),
        // we're going to wait (keep retrying again and again) until it's finally been allocated a place.
        //
        // This is represented by the item being null.
        internal Converter? GetExistingOrAddNull(Type type) => MappingHelpers.GetExistingOrAddNull(_map.AllTypes, type);

        // Adds the current item to the dictionary and fills in its details.
        internal void ApplyItem(Converter item, Type type)
        {
            item.ItemType = type;
            item.IsValueItemType = type.IsValueType;
            item._isGenerating = true;

            lock (_map.AllTypes)
                _map.AllTypes[type] = item;
        }

        static bool TryExpandNullable(ref Type expanded)
        {
            if (expanded.IsGenericType && expanded.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                expanded = expanded.GetGenericArguments()[0];
                return true;
            }

            return false;
        }

        internal void Initialize(ABSaveMap map)
        {
            _map = map;
            _converterCache = new Converter[map.Settings.ConverterCount];
        }

        internal void QueuePropertyForProcessing(MemberAccessorGenerator.PropertyToProcess process) =>
            _propertyAccessorsToProcess.Add(process);

        internal void FinishGeneration() => MemberAccessorGenerator.ProcessAllQueuedAccessors(_propertyAccessorsToProcess);
    }
}