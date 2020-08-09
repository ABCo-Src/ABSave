using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Schema;

namespace ABSoftware.ABSave.Converters
{
    public enum CollectionType
    {
        Array,
        GenericIList,
        Generic,
        NonGenericIList,
        NonGeneric,
        None
    }

    public class ABSaveOnlyICollectionException : Exception
    {
        public ABSaveOnlyICollectionException() : base("ABSAVE: ABSave can only serialize ICollections, plain IEnumerables are not accepted.") { }
    }

    public class CollectionTypeConverter : ABSaveTypeConverter
    {
        public readonly static CollectionTypeConverter Instance = new CollectionTypeConverter();
        private CollectionTypeConverter() { }

        public override bool HasExactType => false;
        public override bool CheckCanConvertType(Type type) => type.IsArray || ABSaveUtils.HasInterface(typeInfo.ActualType, typeof(IEnumerable));

        #region Serialization

        public override void Serialize(object obj, Type type, ABSaveWriter writer)
        {
            if (typeInfo.ActualType.IsArray) SerializeArray((Array)obj, writer, typeInfo.ActualType.GetElementType(), null);
            else
            {
                var collectionType = GetCollectionType(typeInfo.ActualType, out Type specifiedItem);
                if (collectionType == CollectionType.None) throw new ABSaveOnlyICollectionException();

                SerializeByType(obj, writer, specifiedItem, collectionType, null);
            }
        }

        public void Serialize(object obj, ABSaveWriter writer, CollectionMapItem map)
        {
            if (map.CollectionType == CollectionType.Array) SerializeArray((Array)obj, writer, map.ItemType, map.ItemConverter);
            else SerializeByType(obj, writer, map.ItemType, map.CollectionType, map.ItemConverter);
        }

        void SerializeByType(object obj, ABSaveWriter writer, Type specifiedItem, CollectionType collectionType, ABSaveMapItem perItemMap)
        {
            switch (collectionType)
            {
                case CollectionType.Generic:
                case CollectionType.GenericIList:
                    var perItem = GetSerializeCorrectPerItemOperation(specifiedItem, writer.Settings, perItemMap);
                    var asDynamic = (dynamic)obj;

                    var arrSize = asDynamic.Count;
                    writer.WriteInt32((uint)arrSize);

                    if (collectionType == CollectionType.GenericIList)
                        for (int i = 0; i < arrSize; i++) perItem(asDynamic[i], specifiedItem, writer, perItemMap);
                    else
                        foreach (object item in asDynamic) perItem(item, specifiedItem, writer, perItemMap);

                    break;

                case CollectionType.NonGeneric:
                    var asCollection = (ICollection)obj;
                    writer.WriteInt32((uint)asCollection.Count);

                    foreach (object item in asCollection)
                        ABSaveItemConverter.Serialize(item, typeof(object), writer, perItemMap);

                    break;

                case CollectionType.NonGenericIList:
                    var asList = (IList)obj;
                    var size = asList.Count;
                    writer.WriteInt32((uint)size);

                    for (int i = 0; i < size; i++)
                        ABSaveItemConverter.Serialize(asList[i], typeof(object), writer, perItemMap);

                    break;
            }
        }

        #region Array

        void SerializeArray(Array arr, ABSaveWriter writer, Type itemType, ABSaveMapItem perItemMap)
        {
            if (arr.Rank == 1)
                SerializeSingleDimensionalArray(arr, itemType, writer, perItemMap);
            else
                SerializeMultiDimensionalArray(arr, itemType, writer, perItemMap);
        }

        void SerializeSingleDimensionalArray(Array arr, Type itemType, ABSaveWriter writer, ABSaveMapItem perItemMap)
        {
            int lowerBound = arr.GetLowerBound(0);
            var defaultLowerBound = lowerBound == 0;
            writer.WriteByte(defaultLowerBound ? (byte)0 : (byte)1);

            if (!defaultLowerBound) writer.WriteInt32((uint)lowerBound);

            // Fast write bytes or shorts using the writer's native methods.
            if (TryFastWriteArray(arr, writer, itemType)) return;

            writer.WriteInt32((uint)arr.Length);

            var perItem = GetSerializeCorrectPerItemOperation(itemType, writer.Settings, perItemMap);
            int endIndex = lowerBound + arr.Length;

            for (int i = lowerBound; i < endIndex; i++) perItem(arr.GetValue(i), itemType, writer, perItemMap);
        }

        unsafe void SerializeMultiDimensionalArray(Array arr, Type itemType, ABSaveWriter writer, ABSaveMapItem perItemMap)
        {
            int* lowerBounds = stackalloc int[arr.Rank];
            int* lengths = stackalloc int[arr.Rank];

            var defaultLowerBounds = HasDefaultLowerBounds(arr, arr.Rank, lowerBounds);

            // Write the control byte and number of ranks.
            writer.WriteByte(defaultLowerBounds ? (byte)2 : (byte)3);
            writer.WriteInt32((uint)arr.Rank);

            // Write the lower bounds.
            if (!defaultLowerBounds)
            {
                for (int i = 0; i < arr.Rank; i++)
                    writer.WriteInt32((uint)lowerBounds[i]);
            }

            // Write the lengths.
            for (int i = 0; i < arr.Rank; i++)
            {
                lengths[i] = arr.GetLength(i);
                writer.WriteInt32((uint)lengths[i]);
            }

            SerializeDimension(0, lowerBounds, lengths, new int[arr.Rank], arr, itemType, writer, perItemMap);
        }

        unsafe void SerializeDimension(int dimension, int* lowerBounds, int* lengths, int[] currentPos, Array arr, Type itemType, ABSaveWriter reader, ABSaveMapItem perItemMap)
        {
            int lowerBound = lowerBounds[dimension];
            int length = lengths[dimension];

            int endIndex = lowerBound + length;
            int nextDimension = dimension + 1;

            // If this is the deepest we can get, serialize the items in this dimension.
            if (nextDimension == arr.Rank)
            {
                var perItem = GetSerializeCorrectPerItemOperation(itemType, reader.Settings, perItemMap);
                for (currentPos[dimension] = lowerBound; currentPos[dimension] < endIndex; currentPos[dimension]++) perItem(arr.GetValue(currentPos), itemType, reader, perItemMap);
            }
            else
                for (currentPos[dimension] = lowerBound; currentPos[dimension] < endIndex; currentPos[dimension]++)
                    SerializeDimension(nextDimension, lowerBounds, lengths, currentPos, arr, itemType, reader, perItemMap);
        }

        unsafe bool HasDefaultLowerBounds(Array arr, int arrRank, int* lowerBounds)
        {
            bool defaultLowerBound = true;
            for (int i = 0; i < arrRank; i++)
            {
                lowerBounds[i] = arr.GetLowerBound(i);

                if (lowerBounds[i] != 0)
                    defaultLowerBound = false;
            }

            return defaultLowerBound;
        }

        // TODO: BENCHMARK THIS - IS IT REALLY FASTER WITH ALL THE CHECKS?
        unsafe bool TryFastWriteArray(Array arr, ABSaveWriter writer, Type itemType)
        {
            if (itemType == typeof(byte))
                writer.WriteByteArray((byte[])arr, true);

            else if (itemType == typeof(sbyte))
                fixed (sbyte* s = (sbyte[])arr)
                    writer.WriteBytes(new Span<byte>(s, arr.Length), true);

            else if (itemType == typeof(char))
                fixed (char* s = (char[])arr)
                    writer.FastWriteShorts((short*)s, arr.Length);

            else if (itemType == typeof(short))
                fixed (short* s = (short[])arr)
                    writer.FastWriteShorts(s, arr.Length);

            else if (itemType == typeof(ushort))
                fixed (ushort* s = (ushort[])arr)
                    writer.FastWriteShorts((short*)s, arr.Length);
            
            else return false;

            return true;
        }

        #endregion

        #endregion

        #region Deserialization

        public override object Deserialize(Type type, ABSaveReader reader)
        {
            if (type.IsArray) return DeserializeArray(reader, type.GetElementType(), null);
            else
            {
                var collectionType = GetCollectionType(type, out Type specifiedItem);
                if (collectionType == CollectionType.None) throw new ABSaveOnlyICollectionException();

                return DeserializeByType(reader, type, specifiedItem, collectionType, null);
            }
        }

        public object Deserialize(ABSaveReader reader, CollectionMapItem map) 
        {
            if (map.CollectionType == CollectionType.Array) return DeserializeArray(reader, map.ItemType, map.ItemConverter);
            else return DeserializeNonArrayByType(reader, map.ItemType, map.CollectionType, map.ItemConverter);
        }

        object DeserializeNonArrayByType(ABSaveReader reader, Type specifiedItem, CollectionType collectionType, ABSaveMapItem perItemMap)
        {
            switch (collectionType)
            {
                case CollectionType.Generic:
                case CollectionType.GenericIList:
                    var perItem = GetSerializeCorrectPerItemOperation(specifiedItem, writer.Settings, perItemMap);
                    var asDynamic = (dynamic)obj;

                    var arrSize = asDynamic.Count;
                    writer.WriteInt32((uint)arrSize);

                    if (collectionType == CollectionType.GenericIList)
                        for (int i = 0; i < arrSize; i++) perItem(asDynamic[i], specifiedItem, writer, perItemMap);
                    else
                        foreach (object item in asDynamic) perItem(item, specifiedItem, writer, perItemMap);

                    break;

                case CollectionType.NonGeneric:
                    var asCollection = (ICollection)obj;
                    writer.WriteInt32((uint)asCollection.Count);

                    foreach (object item in asCollection)
                        ABSaveItemConverter.Serialize(item, typeof(object), writer, perItemMap);

                    break;

                case CollectionType.NonGenericIList:
                    var asList = (IList)obj;
                    var size = asList.Count;
                    writer.WriteInt32((uint)size);

                    for (int i = 0; i < size; i++)
                        ABSaveItemConverter.Serialize(asList[i], typeof(object), writer, perItemMap);

                    break;
            }
        }

        #region Array

        public object DeserializeArray(ABSaveReader reader, Type itemType, ABSaveMapItem perItemMap)
        {
            var firstByte = reader.ReadByte();
            var isMultidimension = (firstByte & 2) > 0;
            var customLowerBounds = (firstByte & 1) > 0;

            int rank = isMultidimension ? (int)reader.ReadInt32() : 1;

            int[] lowerBounds = null;
            if (customLowerBounds)
            {
                lowerBounds = new int[rank];
                for (int i = 0; i < rank; i++)
                    lowerBounds[i] = (int)reader.ReadInt32();
            }

            if (rank == 1) return DeserializeSingleDimensionalArray(lowerBounds, itemType, reader, perItemMap);
            else return DeserializeMultiDimensionalArray(lowerBounds, rank, itemType, reader, perItemMap);
        }

        object DeserializeSingleDimensionalArray(int[] lowerBounds, Type itemType, ABSaveReader reader, ABSaveMapItem perItemMap)
        {
            // Fast read bytes or shorts using the reader's native methods.
            if (TryFastReadArray(lowerBounds.Length, reader, itemType, out Array arr)) return arr;

            var arrLength = reader.ReadInt32();
            var perItem = GetDeserializeCorrectPerItemOperation(itemType, reader.Settings, perItemMap);

            int lowerBound = 0;
            Array res;

            if (lowerBounds == null)
                res = Array.CreateInstance(itemType, arrLength);
            else
            {
                lowerBound = lowerBounds[0];
                res = Array.CreateInstance(itemType, new int[] { (int)arrLength }, lowerBounds);
            }

            int endIndex = lowerBound + arr.Length;
            for (int i = lowerBound; i < endIndex; i++) res.SetValue(perItem(itemType, reader, perItemMap), i);

            return res;
        }

        unsafe object DeserializeMultiDimensionalArray(int[] lowerBounds, int rank, Type itemType, ABSaveReader reader, ABSaveMapItem perItemMap)
        {
            int[] lengths = new int[rank];
            for (int i = 0; i < rank; i++)
                lengths[i] = (int)reader.ReadInt32();

            Array res = lowerBounds == null ? Array.CreateInstance(itemType, lengths) : Array.CreateInstance(itemType, lengths, lowerBounds);
            DeserializeDimension(0, lowerBounds, lengths, new int[rank], res, itemType, reader, perItemMap);
            return res;
        }

        void DeserializeDimension(int dimension, int[] lowerBounds, int[] lengths, int[] currentPos, Array res, Type itemType, ABSaveReader reader, ABSaveMapItem perItemMap)
        {
            int lowerBound = lowerBounds[dimension];
            int length = lengths[dimension];

            int endIndex = lowerBound + length;
            int nextDimension = dimension + 1;

            // If this is the deepest we can get, deserialize the items in this dimension.
            if (nextDimension == res.Rank)
            {
                var perItem = GetDeserializeCorrectPerItemOperation(itemType, reader.Settings, perItemMap);
                for (currentPos[dimension] = lowerBound; currentPos[dimension] < endIndex; currentPos[dimension]++) res.SetValue(perItem(itemType, reader, perItemMap), currentPos);
            }
            else
            {
                for (currentPos[dimension] = lowerBound; currentPos[dimension] < endIndex; currentPos[dimension]++)
                    DeserializeDimension(nextDimension, lowerBounds, lengths, currentPos, res, itemType, reader, perItemMap);
            }
        }


        unsafe bool TryFastReadArray(int length, ABSaveReader reader, Type itemType, out Array arr)
        {
            if (itemType == typeof(byte))
            {
                arr = new byte[length];
                reader.ReadBytes((byte[])arr);
            }

            if (itemType == typeof(sbyte))
            {
                arr = new sbyte[length];
                fixed (sbyte* arrData = (sbyte[])arr)
                    reader.ReadBytes(new Span<byte>(arrData, length));
            }

            else if (itemType == typeof(char))
            {
                arr = new char[length];
                fixed (char* s = (char[])arr)
                    reader.FastReadShorts((short*)s, (uint)length);
            }
            else if (itemType == typeof(short))
            {
                arr = new short[length];
                fixed (char* s = (char[])arr)
                    reader.FastReadShorts((short*)s, (uint)length);
            }
            else if (itemType == typeof(ushort))
            {
                arr = new ushort[length];
                fixed (ushort* s = (ushort[])arr)
                    reader.FastReadShorts((short*)s, (uint)length);
            }
            else
            {
                arr = null;
                return false;
            }

            return true;
        }

        #endregion

        #endregion

        #region Helpers

        Action<object, Type, ABSaveWriter, ABSaveMapItem> GetSerializeCorrectPerItemOperation(Type itemType, ABSaveSettings settings, ABSaveMapItem perItemMap)
        {
            if (perItemMap != null) 
                if (itemType.IsValueType)
                    return (item, specifiedType, writer, map) => map.Serialize(item, specifiedType, writer);
                else
                    return (item, specifiedType, writer, map) => map.Serialize(item, item.GetType(), writer);

            // If the specified type is a value type, then we know all the items will be the same type, so we only need to find the converter once.
            if (itemType.IsValueType)
                if (ABSaveUtils.TryFindConverterForType(settings, itemType, out ABSaveTypeConverter converter))
                    return (item, specifiedType, writer, map) => converter.Serialize(item, specifiedType, writer);
                else
                    return (item, specifiedType, writer, map) => ABSaveItemConverter.Serialize(item, specifiedType, writer);

            return (item, specifiedType, writer, map) => ABSaveItemConverter.Serialize(item, specifiedType, writer);
        }

        Func<Type, ABSaveReader, ABSaveMapItem, object> GetDeserializeCorrectPerItemOperation(Type itemType, ABSaveSettings settings, ABSaveMapItem perItemMap)
        {
            if (perItemMap != null)
                return (specifiedType, reader, map) => map.Deserialize(specifiedType, reader);

            // If the specified type is a value type, then we know all the items will be the same type, so we only need to find the converter once.
            if (itemType.IsValueType)
                if (ABSaveUtils.TryFindConverterForType(settings, itemType, out ABSaveTypeConverter converter))
                    return (specifiedType, reader, map) => converter.Deserialize(specifiedType, reader);
                else
                    return (specifiedType, reader, map) => ABSaveItemConverter.Deserialize(specifiedType, reader);

            return (specifiedType, reader, map) => ABSaveItemConverter.Deserialize(specifiedType, reader);
        }

        internal CollectionType GetCollectionType(Type type, out Type genericItemType)
        {
            var interfaces = type.GetInterfaces();
            CollectionType detectedType = CollectionType.None;
            Type genericICollection;
            int i = 0;

            for (; i < interfaces.Length; i++)
            {
                // If it's an "IList", then we can instantly return there, as that confirms it's a generic ICollection, which is the best thing to get.
                if (interfaces[i].IsGenericType)
                {
                    var genericTypeDef = interfaces[i].GetGenericTypeDefinition();

                    if (genericTypeDef == typeof(IList<>)) goto ReturnIList;
                    else if (genericTypeDef == typeof(ICollection<>))
                    {
                        genericICollection = interfaces[i++];

                        // Try to see if it's an IList<>.
                        for (; i < interfaces.Length; i++)
                            if (interfaces[i].IsGenericType && interfaces[i].GetGenericTypeDefinition() == typeof(IList<>)) goto ReturnIList;

                        genericItemType = genericICollection.GetGenericArguments()[0];
                        return CollectionType.Generic;
                    }
                }

                else if (interfaces[i] == typeof(IList)) detectedType = CollectionType.NonGenericIList;
                else if (detectedType != CollectionType.NonGenericIList && interfaces[i] == typeof(ICollection)) detectedType = CollectionType.NonGeneric;
            }

            genericItemType = null;
            return detectedType;

        ReturnIList:
            genericItemType = interfaces[i].GetGenericArguments()[0];
            return CollectionType.GenericIList;
        }

        #endregion
    }
}
