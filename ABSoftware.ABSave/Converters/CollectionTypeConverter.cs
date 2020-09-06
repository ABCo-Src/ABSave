using ABSoftware.ABSave.Exceptions;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ABSoftware.ABSave.Converters
{
    public enum CollectionType
    {
        Array,
        GenericICollections,
        NonGenericIList,
        None
    }

    public class CollectionTypeConverter : ABSaveTypeConverter
    {
        public readonly static CollectionTypeConverter Instance = new CollectionTypeConverter();
        private CollectionTypeConverter() { }

        public override bool HasExactType => false;
        public override bool CheckCanConvertType(Type type) => type.IsArray || ABSaveUtils.HasInterface(type, typeof(IEnumerable));

        #region Serialization

        public override void Serialize(object obj, Type type, ABSaveWriter writer)
        {
            var wrapper = GetCollectionWrapper(type);
            SerializeWrapper(obj, wrapper, wrapper.ElementType, writer, null);
        }

        public void Serialize(object obj, ABSaveWriter writer, CollectionMapItem map) => SerializeWrapper(obj, map.CreateWrapper(), map.ElementType, writer, map);

        void SerializeWrapper(object obj, ICollectionWrapper wrapper, Type itemType, ABSaveWriter writer, CollectionMapItem map)
        {
            wrapper.SetCollection(obj);

            var perItem = CollectionHelpers.GetSerializeCorrectPerItemOperation(itemType, writer.Settings, map?.AreElementsSameType);

            var size = wrapper.Count;
            writer.WriteInt32((uint)size);

            foreach (object item in wrapper)
                perItem(item, itemType, writer, map?.PerItem);
        }

        #endregion

        #region Deserialization

        public override object Deserialize(Type type, ABSaveReader reader) 
        {
            var wrapper = GetCollectionWrapper(type);
            return DeserializeWrapper(wrapper, type, wrapper.ElementType, reader, null);
        }
        public object Deserialize(Type type, ABSaveReader reader, CollectionMapItem map) => DeserializeWrapper(map.CreateWrapper(), type, map.ElementType, reader, map);

        object DeserializeWrapper(ICollectionWrapper wrapper, Type type, Type itemType, ABSaveReader reader, CollectionMapItem map)
        {
            var size = (int)reader.ReadInt32();
            var collection = wrapper.CreateCollection(size, type);

            var perItem = CollectionHelpers.GetDeserializeCorrectPerItemOperation(itemType, reader.Settings, map?.PerItem);

            for (int i = 0; i < size; i++)
                wrapper.AddItem(perItem(itemType, reader, map?.PerItem));

            return collection;
        }

        #endregion

        #region Helpers

        internal ICollectionWrapper GetCollectionWrapper(Type type)
        {
            var interfaces = type.GetInterfaces();
            var detectedType = CollectionType.None;

            for (int i = 0; i < interfaces.Length; i++)
            {
                // Determine whether it's a generic ICollection or just an IList.
                if (interfaces[i].IsGenericType && interfaces[i].GetGenericTypeDefinition() == typeof(ICollection<>))
                    return (ICollectionWrapper)Activator.CreateInstance(typeof(GenericICollectionWrapper<>).MakeGenericType(interfaces[i].GetGenericArguments()[0]));

                else if (interfaces[i] == typeof(IList)) detectedType = CollectionType.NonGenericIList;
            }

            if (detectedType == CollectionType.NonGenericIList)
                return new NonGenericIListWrapper();

            throw new ABSaveUnrecognizedCollectionException();
        }

        #endregion
    }
}
