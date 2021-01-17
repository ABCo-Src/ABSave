using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Exceptions;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Pipes;

namespace ABSoftware.ABSave.Converters
{
    public class EnumerableConverter : ABSaveConverter
    {
        public static EnumerableConverter Instance { get; } = new EnumerableConverter();
        private EnumerableConverter() { }

        public override bool ConvertsSubTypes => false;
        public override bool AlsoConvertsNonExact => true;
        public override bool WritesToHeader => true;
        public override Type[] ExactTypes { get; } = new Type[]
        {
            typeof(IEnumerable)
        };

        #region Serialization

        public override void Serialize(object obj, Type actualType, IABSaveConverterContext context, ref BitTarget header)
        {
            var actualContext = (Context)context;

            if (actualContext.Info is ABSaveCollectionInfo collectionInfo)
                SerializeCollection(obj, collectionInfo, actualContext, ref header);
            else if (actualContext.Info is ABSaveDictionaryInfo dictionaryInfo)
                SerializeDictionary(obj, dictionaryInfo, actualContext, ref header);
        }

        void SerializeCollection(object obj, ABSaveCollectionInfo info, Context context, ref BitTarget header)
        {
            var size = info.GetCount(obj);
            header.Serializer.WriteCompressed((uint)size, ref header);

            var enumerator = info.GetEnumerator(obj);
            while (enumerator.MoveNext()) header.Serializer.SerializeItem(enumerator.Current, context.ElementOrKeyMap);
        }

        void SerializeDictionary(object obj, ABSaveDictionaryInfo info, Context context, ref BitTarget header)
        {
            var size = info.GetCount(obj);
            header.Serializer.WriteCompressed((uint)size, ref header);

            var enumerator = info.GetEnumerator(obj);
            while (enumerator.MoveNext())
            {
                header.Serializer.SerializeItem(enumerator.Key, context.ElementOrKeyMap);
                header.Serializer.SerializeItem(enumerator.Value, context.ValueMap);
            }
        }

        #endregion

        #region Deserialization

        public override object Deserialize(Type actualType, IABSaveConverterContext context, ref BitSource header)
        {
            var actualContext = (Context)context;

            if (actualContext.Info is ABSaveCollectionInfo collectionInfo)
                return DeserializeCollection(collectionInfo, actualType, actualContext, ref header);
            else if (actualContext.Info is ABSaveDictionaryInfo dictionaryInfo)
                return DeserializeDictionary(dictionaryInfo, actualType, actualContext, ref header);
            else throw new Exception("Unrecognized enumerable info.");
        }

        object DeserializeCollection(ABSaveCollectionInfo info, Type type, Context context, ref BitSource header)
        {
            int size = (int)header.Deserializer.ReadCompressedInt(ref header);
            var collection = info.CreateCollection(type, size);

            for (int i = 0; i < size; i++)
                info.AddItem(collection, header.Deserializer.DeserializeItem(context.ElementOrKeyMap));

            return collection;
        }

        object DeserializeDictionary(ABSaveDictionaryInfo info, Type type, Context context, ref BitSource header)
        {
            int size = (int)header.Deserializer.ReadCompressedInt(ref header);
            var collection = info.CreateCollection(type, size);

            for (int i = 0; i < size; i++)
            {
                var key = header.Deserializer.DeserializeItem(context.ElementOrKeyMap);
                var value = header.Deserializer.DeserializeItem(context.ValueMap);

                info.AddItem(collection, key, value);
            }

            return collection;
        }

        #endregion

        #region Context

        public override IABSaveConverterContext TryGenerateContext(ABSaveMap map, Type type)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(type)) return null;

            // Try to handle any immediately recognizable types (such as List<> or any direct interfaces).
            {
                if (type.IsGenericType)
                {
                    var gtd = type.GetGenericTypeDefinition();

                    if (gtd == typeof(List<>))
                    {
                        var argType = type.GetGenericArguments()[0];
                        return new Context(ABSaveCollectionInfo.List, argType, map.GetMaptimeSubItem(argType), null, null);
                    }
                    else if (type.IsInterface)
                    {
                        if (gtd == typeof(ICollection<>))
                            return GetContextForCategory(CollectionCategory.GenericICollection, type.GetGenericArguments()[0], null);
                        else if (gtd == typeof(IDictionary<,>))
                            return GetContextForCategory(CollectionCategory.GenericIDictionary, type.GetGenericArguments()[0], type.GetGenericArguments()[1]);
                    }
                }

                if (type.IsInterface)
                {
                    if (type == typeof(IList))
                        return GetContextForCategory(CollectionCategory.NonGenericIList, typeof(object), null);
                    else if (type == typeof(IDictionary))
                        return GetContextForCategory(CollectionCategory.NonGenericIDictionary, typeof(object), null);
                }
            }

            // Work out what category this type falls under.
            CollectionCategory category = DetectCollectionType(type.GetInterfaces(), out Type elementOrKeyType, out Type valueType);
            return GetContextForCategory(category, elementOrKeyType, valueType);

            // Get the correct info for the given type.
            Context GetContextForCategory(CollectionCategory category, Type elementOrKeyType, Type valueType)
            {
                return category switch
                {
                    CollectionCategory.GenericICollection => new Context(ABSaveCollectionInfo.GenericICollection, elementOrKeyType, map.GetMaptimeSubItem(elementOrKeyType), typeof(object), null),
                    CollectionCategory.NonGenericIList => new Context(ABSaveCollectionInfo.NonGenericIList, elementOrKeyType, map.GetMaptimeSubItem(elementOrKeyType), typeof(object), null),
                    CollectionCategory.GenericIDictionary => new Context(ABSaveDictionaryInfo.GenericIDictionary, elementOrKeyType, map.GetMaptimeSubItem(elementOrKeyType), valueType, map.GetMaptimeSubItem(valueType)),
                    CollectionCategory.NonGenericIDictionary => new Context(ABSaveDictionaryInfo.NonGenericIDictionary, elementOrKeyType, map.GetMaptimeSubItem(elementOrKeyType), valueType, map.GetMaptimeSubItem(valueType)),
                    _ => throw new ABSaveUnrecognizedCollectionException(),
                };
            }
            
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

        internal class Context : IABSaveConverterContext // Internal for testing
        {
            public IABSaveEnumerableInfo Info;
            public Type ElementTypeOrKeyType;
            public MapItem ElementOrKeyMap;
            public Type ValueType;
            public MapItem ValueMap;

            public Context(IABSaveEnumerableInfo info, Type elementTypeOrKeyType, MapItem elementOrKeyMap, Type valueType, MapItem valueMap)
            {
                Info = info;
                ElementTypeOrKeyType = elementTypeOrKeyType;
                ElementOrKeyMap = elementOrKeyMap;
                ValueType = valueType;
                ValueMap = valueMap;
            }
        }

        #endregion
    }
}