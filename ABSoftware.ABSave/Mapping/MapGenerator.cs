using ABSoftware.ABSave.Exceptions;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping.Generation;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ABSoftware.ABSave.Mapping
{
    public unsafe class MapGenerator
    {
        internal ABSaveMap Map = null!;
        internal MapItemInfo CurrentItem;

        public MapItemInfo GetMap(Type type)
        {
            // Expand it if it's a nullable.
            bool isNullable = TryExpandNullable(ref type);

            // Try to get it from the dictionary, and start generating if we can't.
            bool alreadyGenerated = GetOrStartGenerating(type, out MapItemInfo pos, Map.GenInfo.AllTypes);
            pos.IsNullable = isNullable;
            if (alreadyGenerated) return pos;

            EnsureTypeSafety(type);

            // Try to use a converter, and fallback to manually mapping an object if we can't.
            if (!ConverterMapper.TryGenerateConvert(type, this, pos))
                GenerateObject(type, this, pos);

            return pos;
        }

        internal MapItemInfo GetRuntimeMap(Type type)
        {
            if (GetOrStartGenerating(type, out MapItemInfo pos, Map.GenInfo.RuntimeMapItems)) return pos;

            ref MapItem item = ref FillItemWith(MapItemType.Runtime, pos);
            item.Extra.RuntimeInnerItem = GetMap(type);
            item.IsGenerating = false;

            pos.IsNullable = item.Extra.RuntimeInnerItem.IsNullable;
            return pos;
        }

        // ABSave Concurrent Generation System:
        //
        // The way this system works is when an item is currently being generated, or is already generated,
        // it will get added to "AllTypes". When added to "AllTypes", it's given a state, these are all the
        // scearios and the states they get assigned.
        //
        // READY:
        // ------
        // The type has been fully generated.
        // 
        // READY:
        // ------
        // If an object is currently in the middle of being generated, it will be put in "AllTypes" under
        // a "Ready" state, because we are able to use items while they're being generated, provided they've
        // been allocated a place already.
        //
        // ALLOCATING:
        // -----------
        // If an object is ABOUT to start generating, but just hasn't quite been allocated a place yet, we're
        // going to wait (keep retrying again and again) until it's finally been allocated a place.
        //
        // PLANNED:
        // -----------
        // If a new type is found in the members of an object, a generator will mark the type and say:
        // "I plan on generating this a little later, right now I just want to get through all the members". 
        // If we encounter a "planned" type that's been marked like this, we can take up the generation ourselves.

        // Try to get the item already generated, if we can't, make a new one with an "Allocating" state.
        //
        // We don't want to allocate inside the lock because that holds EVERYONE up waiting. 
        // So that's why we mark it "Allocating" first, and then finish the
        // allocation below (outside the lock) if needed.

        /// <summary>
        /// Attempts to get the already generated type from the dictionary. If it is currently being generated
        /// on another thread, we will wait for that to complete. If it is not being generated or does not
        /// exist in the dictionary at all, we will start generating it.
        /// </summary>
        /// <returns>Whether it was already generated or not</returns>
        internal bool GetOrStartGenerating(Type type, out MapItemInfo pos, Dictionary<Type, GenMapItemInfo> collection)
        {
            if (TryGetItemFromDict(collection, type, MapItemState.Allocating, out pos))
                return true;

            // Generate a new item, since we failed to get anything from the dict.
            pos = CreateItem(type, collection);
            return false;
        }

        internal static bool TryGetItemFromDict(Dictionary<Type, GenMapItemInfo> collection, Type type, MapItemState stateToAddIfNotIn, out MapItemInfo info)
        {
            info = default;

            while (true)
            {
                // We must lock here to ensure two threads don't both try to generate the same thing twice.
                lock (collection)
                {
                    if (collection.TryGetValue(type, out GenMapItemInfo val))
                    {
                        // Let "Planned" items fall through to generating for ourselves
                        switch (val.State)
                        {
                            case MapItemState.Ready:
                                info = val.Info;
                                return true;
                            case MapItemState.Allocating:
                                goto Retry;
                        }
                    }

                    // Start generating this item.
                    collection[type] = new GenMapItemInfo(stateToAddIfNotIn);
                    return false;
                }

            Retry:
                Thread.Yield(); // Wait a little bit before retrying.
            }
        }

        internal MapItemInfo CreateItem(Type type, Dictionary<Type, GenMapItemInfo> collection)
        {
            ref MapItem item = ref Map.Items.CreateItemAndGet(out NonReallocatingListPos pos);
            item.ItemType = type;
            item.IsValueType = type.IsValueType;
            item.IsGenerating = true;

            var info = new MapItemInfo(pos);

            lock (collection)
                collection[type] = new GenMapItemInfo(info);

            return info;
        }

        internal ref MapItem FillItemWith(MapItemType mapType, MapItemInfo info)
        {
            ref MapItem item = ref Map.Items.GetItemRef(info.Pos);
            item.MapType = mapType;
            return ref item;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void GenerateObject(Type type, MapGenerator gen, MapItemInfo dest)
        {
            ObjectMapper.GenerateNewObject(type, gen, dest);
        }

        internal void Initialize(ABSaveMap map) => Map = map;
    }
}