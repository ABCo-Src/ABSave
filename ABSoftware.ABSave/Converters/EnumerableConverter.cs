using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Exceptions;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Mapping.Generation;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ABSoftware.ABSave.Converters
{
    public class EnumerableConverter : Converter
    {
        public static EnumerableConverter Instance { get; } = new EnumerableConverter();
        private EnumerableConverter() { }

        public override bool AlsoConvertsNonExact => true;
        public override bool UsesHeaderForVersion(uint version) => true;
        public override Type[] ExactTypes { get; } = new Type[]
        {
            typeof(IEnumerable)
        };

        #region Serialization

        public override void Serialize(object obj, Type actualType, ConverterContext context, ref BitTarget header)
        {
            var actualContext = (Context)context;

            if (actualContext.Info is CollectionInfo collectionInfo)
                SerializeCollection(obj, collectionInfo, actualContext, ref header);
            else if (actualContext.Info is DictionaryInfo dictionaryInfo)
                SerializeDictionary(obj, dictionaryInfo, actualContext, ref header);
        }

        static void SerializeCollection(object obj, CollectionInfo info, Context context, ref BitTarget header)
        {
            var size = info.GetCount(obj);
            header.Serializer.WriteCompressed((uint)size, ref header);

            var enumerator = info.GetEnumerator(obj);
            while (enumerator.MoveNext()) header.Serializer.SerializeItem(enumerator.Current, context.ElementOrKeyMap);
        }

        static void SerializeDictionary(object obj, DictionaryInfo info, Context context, ref BitTarget header)
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

        public override object Deserialize(Type actualType, ConverterContext context, ref BitSource header)
        {
            var actualContext = (Context)context;

            if (actualContext.Info is CollectionInfo collectionInfo)
                return DeserializeCollection(collectionInfo, actualType, actualContext, ref header);
            else if (actualContext.Info is DictionaryInfo dictionaryInfo)
                return DeserializeDictionary(dictionaryInfo, actualType, actualContext, ref header);
            else throw new Exception("Unrecognized enumerable info.");
        }

        static object DeserializeCollection(CollectionInfo info, Type type, Context context, ref BitSource header)
        {
            int size = (int)header.Deserializer.ReadCompressedInt(ref header);
            var collection = info.CreateCollection(type, size);

            for (int i = 0; i < size; i++)
                info.AddItem(collection, header.Deserializer.DeserializeItem(context.ElementOrKeyMap));

            return collection;
        }

        static object DeserializeDictionary(DictionaryInfo info, Type type, Context context, ref BitSource header)
        {
            int size = (int)header.Deserializer.ReadCompressedInt(ref header);
            var collection = info.CreateCollection(type, size);

            for (int i = 0; i < size; i++)
            {
                var key = header.Deserializer.DeserializeItem(context.ElementOrKeyMap);
                if (key == null) throw new NullDictionaryKeyException();

                var value = header.Deserializer.DeserializeItem(context.ValueMap);
                info.AddItem(collection, key, value);
            }

            return collection;
        }

        #endregion

        #region Context

        public override void TryGenerateContext(ref ContextGen gen)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(gen.Type)) return;

            var ctx = CreateContext(ref gen);
            gen.AssignContext(ctx, 0);

            // Fill in the context's maps.
            ctx.ElementOrKeyMap = gen.GetMap(ctx.ElementOrKeyType);
            if (ctx.ValueType != null) ctx.ValueMap = gen.GetMap(ctx.ValueType);
        }

        static Context CreateContext(ref ContextGen gen)
        {
            // Try to handle any immediately recognizable types (such as List<> or any direct interfaces).
            {
                if (gen.Type.IsGenericType)
                {
                    var gtd = gen.Type.GetGenericTypeDefinition();

                    if (gtd == typeof(List<>))
                    {
                        var argType = gen.Type.GetGenericArguments()[0];

                        return new Context(CollectionInfo.List, argType, null);
                    }
                    else if (gen.Type.IsInterface)
                    {
                        if (gtd == typeof(ICollection<>))
                            return GetContextForCategory(CollectionCategory.GenericICollection, gen.Type.GetGenericArguments()[0], null);
                        else if (gtd == typeof(IDictionary<,>))
                            return GetContextForCategory(CollectionCategory.GenericIDictionary, gen.Type.GetGenericArguments()[0], gen.Type.GetGenericArguments()[1]);
                    }
                }

                if (gen.Type.IsInterface)
                {
                    if (gen.Type == typeof(IList))
                        return GetContextForCategory(CollectionCategory.NonGenericIList, typeof(object), null);
                    else if (gen.Type == typeof(IDictionary))
                        return GetContextForCategory(CollectionCategory.NonGenericIDictionary, typeof(object), null);
                }
            }

            // Work out what category this type falls under.
            CollectionCategory category = DetectCollectionType(gen.Type.GetInterfaces(), out Type elementOrKeyType, out Type? valueType);
            return GetContextForCategory(category, elementOrKeyType, valueType);

            // Get the correct info for the given type.
            static Context GetContextForCategory(CollectionCategory category, Type elementOrKeyType, Type? valueType)
            {
                return category switch
                {
                    CollectionCategory.GenericICollection => new Context(CollectionInfo.GenericICollection, elementOrKeyType, null),
                    CollectionCategory.NonGenericIList => new Context(CollectionInfo.NonGenericIList, elementOrKeyType, null),
                    CollectionCategory.GenericIDictionary => new Context(DictionaryInfo.GenericIDictionary, elementOrKeyType, valueType),
                    CollectionCategory.NonGenericIDictionary => new Context(DictionaryInfo.NonGenericIDictionary, elementOrKeyType, valueType),
                    _ => throw new UnrecognizedCollectionException(),
                };
            }
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

        internal class Context : ConverterContext // Internal for testing
        {
            public IEnumerableInfo Info;
            public Type ElementOrKeyType;
            public MapItemInfo ElementOrKeyMap;

            // Optional:
            public Type? ValueType;
            public MapItemInfo ValueMap;

            public Context(IEnumerableInfo info, Type elementOrKeyType, Type? valueType)
            {
                Info = info;
                ElementOrKeyType = elementOrKeyType;
                ValueType = valueType;
            }
        }

        #endregion
    }
}