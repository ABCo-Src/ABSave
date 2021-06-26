using ABCo.ABSave.Converters;
using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Mapping.Generation.Object;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ABCo.ABSave.Mapping.Generation
{
    public partial class MapGenerator
    {
        internal ABSaveMap Map = null!;
        internal MapItemInfo CurrentItem;

        // A list of all the property members to still have their accessor processed. These get
        // parallel processed at the very end of the generation process.
        readonly List<MemberAccessorGenerator.PropertyToProcess> _propertyAccessorsToProcess = new List<MemberAccessorGenerator.PropertyToProcess>();

        public MapItemInfo GetMap(Type type)
        {
            bool isNullable = TryExpandNullable(ref type);

            MapItem? existingItem = GetExistingOrAddNull(type);
            if (existingItem != null) return new MapItemInfo(existingItem, isNullable);

            return GenerateMap(type, isNullable);
        }

        MapItemInfo GenerateMap(Type type, bool isNullable)
        {
            EnsureTypeSafety(type);

            MapItem? item = TryGenerateConverter(type);
            if (item == null) throw new UnserializableTypeException(type);

            item._isGenerating = false;
            return new MapItemInfo(item, isNullable);
        }

        internal MapItemInfo GetRuntimeMap(Type type)
        {
            bool isNullable = TryExpandNullable(ref type);

            MapItem? existing = GetExistingOrAddNull(type);
            if (existing is RuntimeMapItem) return new MapItemInfo(existing, isNullable);

            MapItem newItem = existing ?? GenerateMap(type, isNullable).InnerItem;

            // Now wrap it in a runtime item.
            RuntimeMapItem newRuntime;
            lock (Map.AllTypes)
            {
                // Check one more time to make sure the runtime item wasn't generated since we last looked at it.
                if (Map.AllTypes[type] is RuntimeMapItem itm)
                    return new MapItemInfo(itm, isNullable);

                // Generate the new item!
                newRuntime = new RuntimeMapItem(newItem);
                ApplyItemProperties(newRuntime, type);
                Map.AllTypes[type] = newRuntime;
            }

            newRuntime._isGenerating = false;
            return new MapItemInfo(newRuntime, isNullable);
        }

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
        internal MapItem? GetExistingOrAddNull(Type type)
        {
            while (true)
            {
                // We must lock here to ensure two threads don't both try to generate the same thing twice.
                lock (Map.AllTypes)
                {
                    if (Map.AllTypes.TryGetValue(type, out MapItem? val))
                    {
                        // Allocating, try again
                        if (val == null)
                            goto Retry;
                        else
                            return val;
                    }

                    // Start generating this item.
                    Map.AllTypes[type] = null;
                    return null;
                }

            Retry:
                Thread.Yield(); // Wait a little bit before retrying.
            }
        }

        internal MapItem? GetExistingRuntimeOrAddNull(Type type)
        {
            while (true)
            {
                // We must lock here to ensure two threads don't both try to generate the same thing twice.
                lock (Map.AllTypes)
                {
                    if (Map.AllTypes.TryGetValue(type, out MapItem? val))
                    {
                        // Allocating, try again
                        if (val == null)
                            goto Retry;
                    }

                    // Start generating this item.
                    Map.AllTypes[type] = null;
                    return null;
                }

            Retry:
                Thread.Yield(); // Wait a little bit before retrying.
            }
        }

        // Adds the current item to the dictionary and fills in its details.
        internal void ApplyItem(MapItem item, Type type)
        {
            ApplyItemProperties(item, type);

            lock (Map.AllTypes)
                Map.AllTypes[type] = item;
        }

        internal static void ApplyItemProperties(MapItem item, Type type)
        {
            item.ItemType = type;
            item.IsValueItemType = type.IsValueType;
            item._isGenerating = true;
        }

        void EnsureTypeSafety(Type type)
        {
            if (!Map.Settings.BypassDangerousTypeChecking)
            {
                if (type == typeof(object)) throw new DangerousTypeException("an 'object' member");
                if (type == typeof(ValueType)) throw new DangerousTypeException("a 'ValueType' member");
            }
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
            Map = map;
            _converterCache = new Converter[map.Settings.ConverterCount];
        }

        internal void QueuePropertyForProcessing(MemberAccessorGenerator.PropertyToProcess process) =>
            _propertyAccessorsToProcess.Add(process);

        internal void FinishGeneration() => MemberAccessorGenerator.ProcessAllQueuedAccessors(_propertyAccessorsToProcess);
    }
}