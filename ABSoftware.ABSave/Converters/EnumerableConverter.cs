using ABCo.ABSave.Deserialization;
using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Generation;
using ABCo.ABSave.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ABCo.ABSave.Converters
{
    public class EnumerableConverter : Converter
    {
        public IEnumerableInfo Info = null!;
        public Type ElementOrKeyType = null!;
        public MapItemInfo ElementOrKeyMap;

        // Optional:
        public Type? ValueType;
        public MapItemInfo ValueMap;

        public override void Initialize(InitializeInfo info)
        {
            // Try to handle any immediately recognizable types (such as List<> or any direct interfaces).
            TryHandleDirectTypes(info, info.Type);

            // Work out what category this type falls under.
            CollectionCategory category = DetectCollectionType(info.Type.GetInterfaces(), out Type elementOrKeyType, out Type? valueType);
            SetStateFromCategory(info, category, elementOrKeyType, valueType);
        }

        public override bool CheckType(CheckTypeInfo info) => typeof(IEnumerable).IsAssignableFrom(info.Type);

        #region Serialization

        public override void Serialize(object obj, Type actualType, ref BitTarget header)
        {
            if (Info is CollectionInfo collectionInfo)
                SerializeCollection(obj, collectionInfo, ref header);
            else if (Info is DictionaryInfo dictionaryInfo)
                SerializeDictionary(obj, dictionaryInfo, ref header);
        }

        void SerializeCollection(object obj, CollectionInfo info, ref BitTarget header)
        {
            var size = info.GetCount(obj);
            header.Serializer.WriteCompressed((uint)size, ref header);

            var enumerator = info.GetEnumerator(obj);
            while (enumerator.MoveNext()) header.Serializer.SerializeItem(enumerator.Current, ElementOrKeyMap);
        }

        void SerializeDictionary(object obj, DictionaryInfo info, ref BitTarget header)
        {
            var size = info.GetCount(obj);
            header.Serializer.WriteCompressed((uint)size, ref header);

            var enumerator = info.GetEnumerator(obj);
            while (enumerator.MoveNext())
            {
                header.Serializer.SerializeItem(enumerator.Key, ElementOrKeyMap);
                header.Serializer.SerializeItem(enumerator.Value, ValueMap);
            }
        }

        #endregion

        #region Deserialization

        public override object Deserialize(Type actualType, ref BitSource header)
        {
            if (Info is CollectionInfo collectionInfo)
                return DeserializeCollection(collectionInfo, actualType, ref header);
            else if (Info is DictionaryInfo dictionaryInfo)
                return DeserializeDictionary(dictionaryInfo, actualType, ref header);
            else throw new Exception("Unrecognized enumerable info.");
        }

        object DeserializeCollection(CollectionInfo info, Type type, ref BitSource header)
        {
            int size = (int)header.Deserializer.ReadCompressedInt(ref header);
            var collection = info.CreateCollection(type, size);

            for (int i = 0; i < size; i++)
                info.AddItem(collection, header.Deserializer.DeserializeItem(ElementOrKeyMap));

            return collection;
        }

        object DeserializeDictionary(DictionaryInfo info, Type type, ref BitSource header)
        {
            int size = (int)header.Deserializer.ReadCompressedInt(ref header);
            var collection = info.CreateCollection(type, size);

            for (int i = 0; i < size; i++)
            {
                var key = header.Deserializer.DeserializeItem(ElementOrKeyMap);
                if (key == null) throw new NullDictionaryKeyException();

                var value = header.Deserializer.DeserializeItem(ValueMap);
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
            Info = enumerableInfo;
            ElementOrKeyType = elementOrKeyType;
            ValueType = valueType;

            // Fill in the maps.
            ElementOrKeyMap = info.GetMap(elementOrKeyType);
            if (valueType != null) ValueMap = info.GetMap(valueType);
        }

        static CollectionCategory DetectCollectionType(Type[] interfaces, out Type elementOrKeyType, out Type? valueType)
        {
            var category = CollectionCategory.None;
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
                            var args = interfaces[i].GetGenericArguments();
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
                            var genericTypeDef = gtd ?? interfaces[i].GetGenericTypeDefinition(); // "gtd" may be null if we've found the element type.
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
                            var genericTypeDef = gtd ?? interfaces[i].GetGenericTypeDefinition(); // "gtd" may be null if we've found the element type.
                            if (genericTypeDef == typeof(IDictionary<,>))
                            {
                                category = CollectionCategory.GenericIDictionary;

                                // Extract the key and value.
                                var args = interfaces[i].GetGenericArguments();
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

        public override bool AlsoConvertsNonExact => true;
        public override bool UsesHeaderForVersion(uint version) => true;
        public override Type[] ExactTypes { get; } = new Type[]
        {
            typeof(IEnumerable)
        };

        private void TryHandleDirectTypes(InitializeInfo info, Type type)
        {
            if (type.IsGenericType)
            {
                var gtd = type.GetGenericTypeDefinition();

                if (gtd == typeof(List<>))
                {
                    var argType = type.GetGenericArguments()[0];

                    SetState(info, CollectionInfo.List, argType, null);
                }
                else if (type.IsInterface)
                {
                    if (gtd == typeof(ICollection<>))
                        SetStateFromCategory(info, CollectionCategory.GenericICollection, type.GetGenericArguments()[0], null);
                    else if (gtd == typeof(IDictionary<,>))
                        SetStateFromCategory(info, CollectionCategory.GenericIDictionary, type.GetGenericArguments()[0], type.GetGenericArguments()[1]);
                }
            }

            if (type.IsInterface)
            {
                if (type == typeof(IList))
                    SetStateFromCategory(info, CollectionCategory.NonGenericIList, typeof(object), null);
                else if (type == typeof(IDictionary))
                    SetStateFromCategory(info, CollectionCategory.NonGenericIDictionary, typeof(object), null);
            }
        }

        #endregion
    }
}