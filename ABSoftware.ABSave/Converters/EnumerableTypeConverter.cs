using ABSoftware.ABSave.Exceptions;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Pipes;

namespace ABSoftware.ABSave.Converters
{
    public class EnumerableTypeConverter : ABSaveTypeConverter
    {
        public readonly static EnumerableTypeConverter Instance = new EnumerableTypeConverter();
        private EnumerableTypeConverter() { }

        public override bool HasNonExactTypes => true;
        public override bool TryGenerateContext(Type type) => typeof(IEnumerable).IsAssignableFrom(type);

        #region Serialization

        public override void Serialize(object obj, Type type, ABSaveWriter writer)
        {
            var details = GetCollectionDetails(type);

            if (details.Info is ABSaveCollectionInfo collectionInfo)
                SerializeCollection(obj, collectionInfo, details.ElementTypeOrKeyType, writer);
            else if (details.Info is ABSaveDictionaryInfo dictionaryInfo)
                SerializeDictionary(obj, dictionaryInfo, details.ElementTypeOrKeyType, details.ValueType, writer);

        }

        void SerializeCollection(object obj, ABSaveCollectionInfo info, Type itemType, ABSaveWriter writer)
        {
            var perItem = CollectionHelpers.GetSerializePerItemMap(itemType, writer.Settings, out ABSaveTypeConverter converter);

            var size = info.GetCount(obj);
            writer.WriteInt32((uint)size);

            var enumerator = info.GetEnumerator(obj);
            while (enumerator.MoveNext()) perItem(enumerator.Current, itemType, writer, converter, null);
        }

        public void SerializeCollectionMap(object obj, ABSaveWriter writer, CollectionMapItem map)
        {
            var size = map.Info.GetCount(obj);
            writer.WriteInt32((uint)size);

            var enumerator = map.Info.GetEnumerator(obj);
            while (enumerator.MoveNext()) map.PerItem.Serialize(enumerator.Current, map.ElementType, writer);
        }

        void SerializeDictionary(object obj, ABSaveDictionaryInfo info, Type keyType, Type valueType, ABSaveWriter writer)
        {
            var perKey = CollectionHelpers.GetSerializePerItemMap(keyType, writer.Settings, out ABSaveTypeConverter keyConverter);
            var perValue = CollectionHelpers.GetSerializePerItemMap(valueType, writer.Settings, out ABSaveTypeConverter valueConverter);

            var size = info.GetCount(obj);
            writer.WriteInt32((uint)size);

            var enumerator = info.GetEnumerator(obj);
            while (enumerator.MoveNext())
            {
                perKey(enumerator.Key, keyType, writer, keyConverter, null);
                perValue(enumerator.Value, valueType, writer, valueConverter, null);
            }
        }

        public void SerializeDictionaryMap(object obj, ABSaveWriter writer, DictionaryMapItem map)
        {
            var size = map.Info.GetCount(obj);
            writer.WriteInt32((uint)size);

            var enumerator = map.Info.GetEnumerator(obj);
            while (enumerator.MoveNext())
            {
                map.PerKey.Serialize(enumerator.Key, map.KeyType, writer);
                map.PerValue.Serialize(enumerator.Value, map.ValueType, writer);
            }
        }

        #endregion

        #region Deserialization

        public override object Deserialize(Type type, ABSaveReader reader)
        {
            var details = GetCollectionDetails(type);

            if (details.Info is ABSaveCollectionInfo collectionInfo)
                return DeserializeCollection(collectionInfo, type, details.ElementTypeOrKeyType, reader);
            else if (details.Info is ABSaveDictionaryInfo dictionaryInfo)
                return DeserializeDictionary(dictionaryInfo, type, details.ElementTypeOrKeyType, details.ValueType, reader);
            else throw new ABSaveUnrecognizedCollectionException();
        }

        object DeserializeCollection(ABSaveCollectionInfo info, Type collectionType, Type itemType, ABSaveReader reader)
        {
            var perItem = CollectionHelpers.GetDeserializePerItemAction(itemType, reader.Settings, out ABSaveTypeConverter converter);

            var size = (int)reader.ReadInt32();
            var collection = info.CreateCollection(collectionType, size);

            for (int i = 0; i < size; i++)
                info.AddItem(collection, perItem(itemType, reader, converter, null));

            return collection;
        }

        public object DeserializeCollectionMap(Type collectionType, ABSaveReader reader, CollectionMapItem map)
        {
            var size = (int)reader.ReadInt32();
            var collection = map.Info.CreateCollection(collectionType, size);

            for (int i = 0; i < size; i++)
                map.Info.AddItem(collection, map.PerItem.Deserialize(map.ElementType, reader));

            return collection;
        }

        object DeserializeDictionary(ABSaveDictionaryInfo info, Type type, Type keyType, Type valueType, ABSaveReader reader)
        {
            var perKey = CollectionHelpers.GetDeserializePerItemAction(keyType, reader.Settings, out ABSaveTypeConverter keyConverter);
            var perValue = CollectionHelpers.GetDeserializePerItemAction(valueType, reader.Settings, out ABSaveTypeConverter valueConverter);

            var size = (int)reader.ReadInt32();
            var collection = info.CreateCollection(type, size);

            for (int i = 0; i < size; i++)
            {
                var key = perKey(keyType, reader, keyConverter, null);
                var value = perValue(valueType, reader, valueConverter, null);

                info.AddItem(collection, key, value);
            }

            return collection;
        }

        public object DeserializeDictionaryMap(Type type, ABSaveReader reader, DictionaryMapItem map)
        {
            var size = (int)reader.ReadInt32();
            var collection = map.Info.CreateCollection(type, size);

            for (int i = 0; i < size; i++)
            {
                var key = map.PerKey.Deserialize(map.KeyType, reader);
                var value = map.PerValue.Deserialize(map.ValueType, reader);

                map.Info.AddItem(collection, key, value);
            }

            return collection;
        }

        #endregion

        #region Helpers

        public CollectionDetails GetCollectionDetails(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) return new CollectionDetails(ABSaveCollectionInfo.NonGenericIList, type.GetGenericArguments()[0], typeof(object));

            CollectionCategory category = DetectCollectionType(type.GetInterfaces(), out Type elementOrKeyType, out Type valueType);

            // Get the correct info for the given type.
            return category switch
            {
                CollectionCategory.GenericICollection => new CollectionDetails(ABSaveCollectionInfo.GenericICollection, elementOrKeyType, typeof(object)),
                CollectionCategory.NonGenericIList => new CollectionDetails(ABSaveCollectionInfo.NonGenericIList, elementOrKeyType, typeof(object)),
                CollectionCategory.GenericIDictionary => new CollectionDetails(ABSaveDictionaryInfo.GenericIDictionary, elementOrKeyType, valueType),
                CollectionCategory.NonGenericIDictionary => new CollectionDetails(ABSaveDictionaryInfo.NonGenericIDictionary, elementOrKeyType, valueType),
                _ => throw new ABSaveUnrecognizedCollectionException(),
            };
        }

        static CollectionCategory DetectCollectionType(Type[] interfaces, out Type elementOrKeyType, out Type valueType)
        {
            var category = CollectionCategory.None;
            bool needsToFindICollection = true;
            Type genericICollectionType = null;
            elementOrKeyType = null;
            valueType = null;

            for (int i = 0; i < interfaces.Length; i++)
            {
                Type gtd = null;

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

        #endregion
    }
}
