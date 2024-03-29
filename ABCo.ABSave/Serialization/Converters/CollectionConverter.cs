﻿using ABCo.ABSave.Serialization.Reading;
using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Mapping.Generation.Converters;
using ABCo.ABSave.Serialization.Writing;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ABCo.ABSave.Serialization.Converters
{
    [Select(typeof(ICollection<>), 0)]
    [Select(typeof(IList<>), 0)]
    [Select(typeof(IDictionary<,>), 0, 1)]
    [Select(typeof(List<>), 0)]
    [Select(typeof(Dictionary<,>), 0, 1)]
    [SelectOtherWithCheckType]
    public class CollectionConverter : Converter
    {
        public IEnumerableInfo _info = null!;
        public Type _elementOrKeyType = null!;
        public MapItemInfo _elementOrKeyMap;

        // Optional:
        public Type? _valueType;
        public MapItemInfo _valueMap;

        public override uint Initialize(InitializeInfo info)
        {
	        if (!info.Type.IsPublic) throw new InaccessibleTypeException(info.Type);

	        // Try to handle any immediately recognizable types (such as List<> or any direct interfaces).
            if (TryHandleDirectTypes(info, info.Type)) return 0;

            // Work out what category this type falls under.
            CollectionCategory category = DetectCollectionType(info.Type.GetInterfaces(), out Type elementOrKeyType, out Type? valueType);
            SetStateFromCategory(info, category, elementOrKeyType, valueType);
            return 0;
        }

        public override bool CheckType(CheckTypeInfo info) => typeof(IEnumerable).IsAssignableFrom(info.Type);

        #region Serialization

        public override void Serialize(in SerializeInfo info)
        {
            if (_info is CollectionInfo collectionInfo)
                SerializeCollection(info.Instance, collectionInfo, info.Serializer);
            else if (_info is DictionaryInfo dictionaryInfo)
                SerializeDictionary(info.Instance, dictionaryInfo, info.Serializer);
        }

        void SerializeCollection(object obj, CollectionInfo info, ABSaveSerializer serializer)
        {
            int size = info.GetCount(obj);
            serializer.WriteCompressedInt((uint)size);

            IEnumerator? enumerator = info.GetEnumerator(obj);
            try
            {
                while (enumerator.MoveNext()) serializer.WriteItem(enumerator.Current, _elementOrKeyMap);
            }
            finally
            {
                if (enumerator is IDisposable disp) disp.Dispose();
            }
        }

        void SerializeDictionary(object obj, DictionaryInfo info, ABSaveSerializer serializer)
        {
            int size = info.GetCount(obj);
            serializer.WriteCompressedInt((uint)size);

            IDictionaryEnumerator? enumerator = info.GetEnumerator(obj);
            try
            {
                while (enumerator.MoveNext())
                {
                    serializer.WriteItem(enumerator.Key, _elementOrKeyMap);
                    serializer.WriteItem(enumerator.Value, _valueMap);
                }
            }
            finally
            {
                if (enumerator is IDisposable disp) disp.Dispose();
            }
        }

        #endregion

        #region Deserialization

        public override object Deserialize(in DeserializeInfo info)
        {
            if (_info is CollectionInfo collectionInfo)
                return DeserializeCollection(collectionInfo, info.ActualType, info.Deserializer);
            else if (_info is DictionaryInfo dictionaryInfo)
                return DeserializeDictionary(dictionaryInfo, info.ActualType, info.Deserializer);
            else throw new Exception("Unrecognized enumerable info.");
        }

        object DeserializeCollection(CollectionInfo info, Type type, ABSaveDeserializer deserializer)
        {
            int size = (int)deserializer.ReadCompressedInt();
            object? collection = info.CreateCollection(type, size);

            for (int i = 0; i < size; i++)
                info.AddItem(collection, deserializer.ReadItem(_elementOrKeyMap));

            return collection;
        }

        object DeserializeDictionary(DictionaryInfo info, Type type, ABSaveDeserializer deserializer)
        {
            int size = (int)deserializer.ReadCompressedInt();
            object? collection = info.CreateCollection(type, size);

            for (int i = 0; i < size; i++)
            {
                object? key = deserializer.ReadItem(_elementOrKeyMap);
                if (key == null) throw new NullDictionaryKeyException();

                object? value = deserializer.ReadItem(_valueMap);
                info.AddItem(collection, key, value);
            }

            return collection;
        }

        #endregion

        #region Context

        void SetStateFromCategory(InitializeInfo info, CollectionCategory category, Type elementOrKeyType, Type? valueType)
        {
            IEnumerableInfo enumerableInfo = category switch
            {
                CollectionCategory.GenericICollection => CollectionInfo.GenericICollection,
                CollectionCategory.NonGenericIList => CollectionInfo.NonGenericIList,
                CollectionCategory.GenericIDictionary => DictionaryInfo.GenericIDictionary,
                CollectionCategory.NonGenericIDictionary => DictionaryInfo.NonGenericIDictionary,
                _ => throw new Exception("Invalid collection category")
            };

            SetState(info, enumerableInfo, elementOrKeyType, valueType);
        }

        void SetState(InitializeInfo info, IEnumerableInfo enumerableInfo, Type elementOrKeyType, Type? valueType)
        {
            _info = enumerableInfo;
            _elementOrKeyType = elementOrKeyType;
            _valueType = valueType;

            // Fill in the maps.
            _elementOrKeyMap = info.GetMap(elementOrKeyType);
            if (valueType != null) _valueMap = info.GetMap(valueType);
        }

        static CollectionCategory DetectCollectionType(Type[] interfaces, out Type elementOrKeyType, out Type? valueType)
        {
            CollectionCategory category = CollectionCategory.None;
            bool needsToFindICollection = true;
            Type? genericICollectionType = null;
            elementOrKeyType = null!;
            valueType = null;

            for (int i = 0; i < interfaces.Length; i++)
            {
                Type? gtd = null;

                // If we haven't found the "ICollection<>" this collection has yet, try to get it. This will help us extract the type if there isn't anything more specific.
                if (needsToFindICollection && interfaces[i].IsGenericType)
                {
                    gtd = interfaces[i].GetGenericTypeDefinition();

                    if (gtd == typeof(ICollection<>))
                    {
                        needsToFindICollection = false;
                        genericICollectionType = interfaces[i];

                        if (category == CollectionCategory.None) category = CollectionCategory.GenericICollection;
                        continue;
                    }
                }

                // Update the category if necessary.
                switch (category)
                {
                    case CollectionCategory.NonGenericIDictionary:

                        // Try to see if there's a generic variant to get the types from.
                        if (interfaces[i].IsGenericType && interfaces[i].GetGenericTypeDefinition() == typeof(IDictionary<,>))
                        {
                            Type[]? args = interfaces[i].GetGenericArguments();
                            elementOrKeyType = args[0];
                            valueType = args[1];

                            return CollectionCategory.NonGenericIDictionary;
                        }

                        break;

                    case CollectionCategory.GenericIDictionary:

                        if (interfaces[i] == typeof(IDictionary))
                        {
                            // We have everything we need: We have the key/value types and we know it's also got a non-generic varient we can use for better performance, so we can stop here.
                            return CollectionCategory.NonGenericIDictionary;
                        }

                        break;

                    case CollectionCategory.NonGenericIList:

                        if (interfaces[i] == typeof(IDictionary)) goto SwitchToIDictionary;
                        else if (interfaces[i].IsGenericType)
                        {
                            Type? genericTypeDef = gtd ?? interfaces[i].GetGenericTypeDefinition(); // "gtd" may be null if we've found the element type.
                            if (genericTypeDef == typeof(IDictionary<,>)) category = CollectionCategory.GenericIDictionary;
                        }

                        break;

                    case CollectionCategory.GenericICollection:
                    case CollectionCategory.None:

                        if (interfaces[i] == typeof(IList)) category = CollectionCategory.NonGenericIList;
                        else if (interfaces[i] == typeof(IDictionary)) goto SwitchToIDictionary;

                        // Generic IDictionary
                        else if (interfaces[i].IsGenericType)
                        {
                            Type? genericTypeDef = gtd ?? interfaces[i].GetGenericTypeDefinition(); // "gtd" may be null if we've found the element type.
                            if (genericTypeDef == typeof(IDictionary<,>))
                            {
                                category = CollectionCategory.GenericIDictionary;

                                // Extract the key and value.
                                Type[]? args = interfaces[i].GetGenericArguments();
                                elementOrKeyType = args[0];
                                valueType = args[1];
                            }
                        }

                        break;
                }

                continue;

            SwitchToIDictionary:
                needsToFindICollection = false;
                valueType = elementOrKeyType = typeof(object);
                category = CollectionCategory.NonGenericIDictionary;
            }

            // Try to extract the element type from the "ICollection<>" we found if we don't have one.
            if (elementOrKeyType == null)
            {
                if (genericICollectionType == null) elementOrKeyType = typeof(object);
                else elementOrKeyType = genericICollectionType.GetGenericArguments()[0];
            }

            return category;
        }

        enum CollectionCategory
        {
            GenericIEnumerable,
            GenericIDictionary,
            NonGenericIDictionary,
            GenericICollection,
            NonGenericIList,
            None
        }

        private bool TryHandleDirectTypes(InitializeInfo info, Type type)
        {
            if (type.IsGenericType)
            {
                Type? gtd = type.GetGenericTypeDefinition();

                if (gtd == typeof(List<>))
                {
                    Type? argType = type.GetGenericArguments()[0];

                    SetState(info, CollectionInfo.List, argType, null);
                    return true;
                }
                else if (type.IsInterface)
                {
                    if (gtd == typeof(ICollection<>))
                    {
                        SetStateFromCategory(info, CollectionCategory.GenericICollection, type.GetGenericArguments()[0], null);
                        return true;
                    }
                    else if (gtd == typeof(IDictionary<,>))
                    {
                        SetStateFromCategory(info, CollectionCategory.GenericIDictionary, type.GetGenericArguments()[0], type.GetGenericArguments()[1]);
                        return true;
                    }
                }
            }

            if (type.IsInterface)
            {
                if (type == typeof(IList))
                {
                    SetStateFromCategory(info, CollectionCategory.NonGenericIList, typeof(object), null);
                    return true;
                }
                else if (type == typeof(IDictionary))
                {
                    SetStateFromCategory(info, CollectionCategory.NonGenericIDictionary, typeof(object), null);
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}