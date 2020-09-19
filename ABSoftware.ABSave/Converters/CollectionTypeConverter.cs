using ABSoftware.ABSave.Exceptions;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ABSoftware.ABSave.Converters
{
    public class CollectionTypeConverter : ABSaveTypeConverter
    {
        public readonly static CollectionTypeConverter Instance = new CollectionTypeConverter();
        private CollectionTypeConverter() { }

        public override bool HasExactType => false;
        public override bool CheckCanConvertType(Type type) => typeof(IEnumerable).IsAssignableFrom(type);

        #region Serialization

        public override void Serialize(object obj, Type type, ABSaveWriter writer)
        {
            var info = GetCollectionInfo(type, out Type elementType);
            SerializeWrapper(obj, info, elementType, writer, null);
        }

        public void Serialize(object obj, ABSaveWriter writer, CollectionMapItem map) => SerializeWrapper(obj, map.Info, map.ElementType, writer, map);

        void SerializeWrapper(object obj, CollectionInfo info, Type itemType, ABSaveWriter writer, CollectionMapItem map)
        {
            var enumerable = (IEnumerable)obj;

            var perItem = CollectionHelpers.GetSerializeCorrectPerItemOperation(itemType, writer.Settings, map?.AreElementsSameType);
            var perItemMap = map?.PerItem;

            var size = info.GetCount(obj);
            writer.WriteInt32((uint)size);

            foreach (object item in enumerable)
                perItem(item, itemType, writer, perItemMap);
        }

        #endregion

        #region Deserialization

        public override object Deserialize(Type type, ABSaveReader reader) 
        {
            var info = GetCollectionInfo(type, out Type elementType);
            return DeserializeWrapper(info, type, elementType, reader, null);
        }
        public object Deserialize(Type type, ABSaveReader reader, CollectionMapItem map) => DeserializeWrapper(map.Info, type, map.ElementType, reader, map);

        object DeserializeWrapper(CollectionInfo info, Type type, Type itemType, ABSaveReader reader, CollectionMapItem map)
        {
            var size = (int)reader.ReadInt32();
            var collection = info.CreateCollection(type, size);

            var perItem = CollectionHelpers.GetDeserializeCorrectPerItemOperation(itemType, reader.Settings, map?.PerItem);
            var perItemMap = map?.PerItem;

            for (int i = 0; i < size; i++)
                info.AddItem(collection, perItem(itemType, reader, perItemMap));

            return collection;
        }

        #endregion

        #region Helpers

        internal CollectionInfo GetCollectionInfo(Type type, out Type elementType)
        {
            DetectedType detectedType = DetectCollectionType(type.GetInterfaces(), new DetectedType(CollectionCategory.None, null));

            // Get the correct info for the given type.
            switch (detectedType.TypeCategory)
            {
                case CollectionCategory.GenericICollection:
                    elementType = detectedType.FullType.GetGenericArguments()[0];
                    return CollectionInfo.GenericICollection;

                case CollectionCategory.NonGenericIList:
                    elementType = typeof(object);
                    return CollectionInfo.NonGenericIList;

                case CollectionCategory.GenericIDictionary:
                    elementType = type.GetInterface(typeof(IEnumerable<>).Name).GetGenericArguments()[0]; // Consider optimizing?
                    return CollectionInfo.GenericIDictionary;

                case CollectionCategory.NonGenericIDictionary:
                    elementType = typeof(DictionaryEntry);
                    return CollectionInfo.NonGenericIDictionary;

                default:
                    throw new ABSaveUnrecognizedCollectionException();
            }
        }

        static DetectedType DetectCollectionType(Type[] interfaces, DetectedType detectedType)
        {
            for (int i = 0; i < interfaces.Length; i++)
            {
                switch (detectedType.TypeCategory)
                {
                    case CollectionCategory.NonGenericIDictionary:

                        if (interfaces[i].IsGenericType && interfaces[i].GetGenericTypeDefinition() == typeof(IDictionary<,>)) return new DetectedType(CollectionCategory.GenericIDictionary, interfaces[i]);

                        break;

                    case CollectionCategory.GenericICollection:

                        if (interfaces[i].IsGenericType && interfaces[i].GetGenericTypeDefinition() == typeof(IDictionary<,>)) return new DetectedType(CollectionCategory.GenericIDictionary, interfaces[i]);
                        else if (interfaces[i] == typeof(IDictionary)) detectedType = new DetectedType(CollectionCategory.NonGenericIDictionary, interfaces[i]);

                        break;

                    case CollectionCategory.None:

                        if (!interfaces[i].IsGenericType && interfaces[i] == typeof(IList)) detectedType = new DetectedType(CollectionCategory.NonGenericIList, interfaces[i]);
                        else goto case CollectionCategory.NonGenericIList;

                        break;

                    case CollectionCategory.NonGenericIList:

                        if (interfaces[i].IsGenericType)
                        {
                            var gtd = interfaces[i].GetGenericTypeDefinition();

                            if (gtd == typeof(ICollection<>)) detectedType = new DetectedType(CollectionCategory.GenericICollection, interfaces[i]);
                            else if (gtd == typeof(IDictionary<,>)) return new DetectedType(CollectionCategory.GenericIDictionary, interfaces[i]);
                        }
                        else if (interfaces[i] == typeof(IDictionary)) detectedType = new DetectedType(CollectionCategory.NonGenericIDictionary, interfaces[i]);

                        break;
                }
            }

            return detectedType;
        }

        struct DetectedType
        {
            public CollectionCategory TypeCategory;
            public Type FullType;

            public DetectedType(CollectionCategory typeCategory, Type fullType)
            {
                TypeCategory = typeCategory;
                FullType = fullType;
            }
        }

        enum CollectionCategory
        {
            GenericIDictionary,
            NonGenericIDictionary,
            GenericICollection,
            NonGenericIList,
            None
        }

        #endregion
    }
}
