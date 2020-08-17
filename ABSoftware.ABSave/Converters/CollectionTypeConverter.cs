using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Exceptions;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Serialization;
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
            if (type.IsArray) SerializeArray((Array)obj, writer, type.GetElementType(), null);
            else SerializeWrapper(obj, GetCollectionWrapper(type), writer, null);
        }

        public void Serialize(object obj, ABSaveWriter writer, CollectionMapItem map)
        {
            if (map.IsArray) SerializeArray((Array)obj, writer, map.ArrayElementType, map.PerItem);
            else SerializeWrapper(obj, map.CreateWrapper(), writer, map.PerItem);
        }

        void SerializeWrapper(object obj, ICollectionWrapper wrapper, ABSaveWriter writer, ABSaveMapItem perItemMap)
        {
            wrapper.SetCollection(obj);

            var itemType = wrapper.ElementType;
            var perItem = GetSerializeCorrectPerItemOperation(itemType, writer.Settings, perItemMap);

            var size = wrapper.Count;
            writer.WriteInt32((uint)size);

            foreach (object item in wrapper)
                perItem(item, itemType, writer, perItemMap);
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
            int* lengths = stackalloc int[arr.Rank];
            var lowerBounds = GetLowerBounds(arr, arr.Rank, out bool defaultLowerBounds);

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

            SerializeDimension(0, lengths, lowerBounds, arr, itemType, writer, perItemMap);
        }

        unsafe void SerializeDimension(int dimension, int* lengths, int[] currentPos, Array arr, Type itemType, ABSaveWriter reader, ABSaveMapItem perItemMap)
        {
            int oldPos = currentPos[dimension];

            int endIndex = currentPos[dimension] + lengths[dimension];
            int nextDimension = dimension + 1;

            // If this is the deepest we can get, serialize the items in this dimension.
            if (nextDimension == arr.Rank)
            {
                var perItem = GetSerializeCorrectPerItemOperation(itemType, reader.Settings, perItemMap);
                for (; currentPos[dimension] < endIndex; currentPos[dimension]++) perItem(arr.GetValue(currentPos), itemType, reader, perItemMap);
            }
            else
                for (; currentPos[dimension] < endIndex; currentPos[dimension]++)
                    SerializeDimension(nextDimension, lengths, currentPos, arr, itemType, reader, perItemMap);

            currentPos[dimension] = oldPos;
        }

        unsafe int[] GetLowerBounds(Array arr, int arrRank, out bool defaultLowerBounds)
        {
            var res = new int[arrRank];

            defaultLowerBounds = true;
            for (int i = 0; i < arrRank; i++)
            {
                res[i] = arr.GetLowerBound(i);

                if (res[i] != 0)
                    defaultLowerBounds = false;
            }

            return res;
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
            else return DeserializeWrapper(GetCollectionWrapper(type), type, reader, null);
        }

        public object Deserialize(Type type, ABSaveReader reader, CollectionMapItem map) 
        {
            if (map.IsArray) return DeserializeArray(reader, type, map.PerItem);
            else return DeserializeWrapper(map.CreateWrapper(), type, reader, map.PerItem);
        }

        object DeserializeWrapper(ICollectionWrapper wrapper, Type type, ABSaveReader reader, ABSaveMapItem perItemMap)
        {
            var size = (int)reader.ReadInt32();
            var collection = wrapper.CreateCollection(size, type);

            var perItem = GetDeserializeCorrectPerItemOperation(wrapper.ElementType, reader.Settings, perItemMap);

            for (int i = 0; i < size; i++)
                wrapper.AddItem(perItem(type, reader, perItemMap));

            return collection;
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
            DeserializeDimension(0, lengths, lowerBounds, res, itemType, reader, perItemMap);
            return res;
        }

        void DeserializeDimension(int dimension, int[] lengths, int[] currentPos, Array res, Type itemType, ABSaveReader reader, ABSaveMapItem perItemMap)
        {
            int oldPos = currentPos[dimension];

            int endIndex = currentPos[dimension] + lengths[dimension];
            int nextDimension = dimension + 1;

            // If this is the deepest we can get, deserialize the items in this dimension.
            if (nextDimension == res.Rank)
            {
                var perItem = GetDeserializeCorrectPerItemOperation(itemType, reader.Settings, perItemMap);
                for (; currentPos[dimension] < endIndex; currentPos[dimension]++) res.SetValue(perItem(itemType, reader, perItemMap), currentPos);
            }
            else
                for (; currentPos[dimension] < endIndex; currentPos[dimension]++)
                    DeserializeDimension(nextDimension, lengths, currentPos, res, itemType, reader, perItemMap);

            currentPos[dimension] = oldPos;
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
                    return (item, specifiedType, writer, map) => ABSaveItemConverter.SerializeWithAttribute(item, specifiedType, writer);

            return (item, specifiedType, writer, map) => ABSaveItemConverter.SerializeWithAttribute(item, specifiedType, writer);
        }

        Func<Type, ABSaveReader, ABSaveMapItem, object> GetDeserializeCorrectPerItemOperation(Type itemType, ABSaveSettings settings, ABSaveMapItem perItemMap)
        {
            if (perItemMap != null)
                return (specifiedType, reader, map) => map.Deserialize(specifiedType, reader);

            // If the specified type is a value type and there's a converter for it, then we know all the items will be the same type, so we only need to find the converter once.
            if (itemType.IsValueType && ABSaveUtils.TryFindConverterForType(settings, itemType, out ABSaveTypeConverter converter))
                return (specifiedType, reader, map) => converter.Deserialize(specifiedType, reader);

            return (specifiedType, reader, map) => ABSaveItemConverter.DeserializeWithAttribute(specifiedType, reader);
        }

        internal ICollectionWrapper GetCollectionWrapper(Type type)
        {
            var interfaces = type.GetInterfaces();
            var detectedType = CollectionType.None;

            for (int i = 0; i < interfaces.Length; i++)
            {
                // If it's an "IList", then we can instantly return there, as that confirms it's a generic ICollection, which is the best thing to get.
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
