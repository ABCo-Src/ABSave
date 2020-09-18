using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Converters
{
    public class ArrayTypeConverter : ABSaveTypeConverter
    {
        public readonly static ArrayTypeConverter Instance = new ArrayTypeConverter();
        private ArrayTypeConverter() { }

        public override bool HasExactType => false;
        public override bool CheckCanConvertType(Type type) => type.IsArray;

        #region Serialization

        public override void Serialize(object obj, Type type, ABSaveWriter writer) => SerializeArray((Array)obj, writer, type.GetElementType(), null);
        public void Serialize(Array obj, ABSaveWriter writer, ArrayMapItem map) => SerializeArray(obj, writer, map.ElementType, map);

        void SerializeArray(Array arr, ABSaveWriter writer, Type itemType, ArrayMapItem map)
        {
            if (arr.Rank == 1) SerializeSingleDimensionalArray(arr, itemType, writer, map);
            else SerializeMultiDimensionalArray(arr, itemType, writer, map);
        }

        void SerializeSingleDimensionalArray(Array arr, Type itemType, ABSaveWriter writer, ArrayMapItem map)
        {
            int lowerBound = arr.GetLowerBound(0);
            var defaultLowerBound = lowerBound == 0;
            writer.WriteByte(defaultLowerBound ? (byte)0 : (byte)1);

            if (!defaultLowerBound) writer.WriteInt32((uint)lowerBound);

            // Fast write bytes or shorts using the writer's native methods.
            if (TryFastWriteArray(arr, writer, itemType)) return;

            writer.WriteInt32((uint)arr.Length);

            var perItem = CollectionHelpers.GetSerializeCorrectPerItemOperation(itemType, writer.Settings, map?.AreElementsSameType);
            int endIndex = lowerBound + arr.Length;

            for (int i = lowerBound; i < endIndex; i++) perItem(arr.GetValue(i), itemType, writer, map?.PerItem);
        }

        unsafe void SerializeMultiDimensionalArray(Array arr, Type itemType, ABSaveWriter writer, ArrayMapItem map)
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

            SerializeDimension(0, lengths, lowerBounds, arr, itemType, writer, map);
        }

        unsafe void SerializeDimension(int dimension, int* lengths, int[] currentPos, Array arr, Type itemType, ABSaveWriter reader, ArrayMapItem map)
        {
            int oldPos = currentPos[dimension];

            int endIndex = currentPos[dimension] + lengths[dimension];
            int nextDimension = dimension + 1;

            // If this is the deepest we can get, serialize the items in this dimension.
            if (nextDimension == arr.Rank)
            {
                var perItem = CollectionHelpers.GetSerializeCorrectPerItemOperation(itemType, reader.Settings, map?.AreElementsSameType);
                for (; currentPos[dimension] < endIndex; currentPos[dimension]++) perItem(arr.GetValue(currentPos), itemType, reader, map?.PerItem);
            }
            else
                for (; currentPos[dimension] < endIndex; currentPos[dimension]++)
                    SerializeDimension(nextDimension, lengths, currentPos, arr, itemType, reader, map);

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
            switch (Type.GetTypeCode(itemType))
            {
                case TypeCode.Byte:
                    writer.WriteByteArray((byte[])arr, true);
                    return true;
                case TypeCode.SByte:
                    fixed (sbyte* s = (sbyte[])arr)
                        writer.WriteBytes(new Span<byte>(s, arr.Length), true);

                    return true;
                case TypeCode.Char:
                    writer.WriteCharArray((char[])arr);
                    return true;
                case TypeCode.Int16:
                    fixed (short* s = (short[])arr)
                    {
                        writer.WriteInt32((uint)arr.Length);
                        writer.FastWriteShorts((ushort*)s, arr.Length);
                    }

                    return true;
                case TypeCode.UInt16:
                    fixed (ushort* s = (ushort[])arr)
                    {
                        writer.WriteInt32((uint)arr.Length);
                        writer.FastWriteShorts(s, arr.Length);
                    }

                    return true;
                default:
                    return false;
            }
        }

        #endregion

        #region Deserialization

        public override object Deserialize(Type type, ABSaveReader reader) => DeserializeArray(type.GetElementType(), reader, null);
        public Array Deserialize(ABSaveReader reader, ArrayMapItem map) => (Array)DeserializeArray(map.ElementType, reader, map);

        public object DeserializeArray(Type elementType, ABSaveReader reader, ArrayMapItem map)
        {
            var firstByte = reader.ReadByte();
            var isMultidimension = (firstByte & 2) > 0;
            var hasCustomLowerBounds = (firstByte & 1) > 0;

            int rank = isMultidimension ? (int)reader.ReadInt32() : 1;

            int[] lowerBounds = null;
            if (hasCustomLowerBounds)
            {
                lowerBounds = new int[rank];
                for (int i = 0; i < rank; i++)
                    lowerBounds[i] = (int)reader.ReadInt32();
            }

            if (rank == 1) return DeserializeSingleDimensionalArray(lowerBounds, elementType, reader, map?.PerItem);
            else return DeserializeMultiDimensionalArray(lowerBounds, rank, elementType, reader, map?.PerItem);
        }

        object DeserializeSingleDimensionalArray(int[] lowerBounds, Type itemType, ABSaveReader reader, ABSaveMapItem perItemMap)
        {
            var arrLength = (int)reader.ReadInt32();

            if (TryFastReadArray(arrLength, reader, itemType, out Array arr)) return arr;

            var perItem = CollectionHelpers.GetDeserializeCorrectPerItemOperation(itemType, reader.Settings, perItemMap);

            int lowerBound = 0;
            Array res;

            if (lowerBounds == null)
                res = Array.CreateInstance(itemType, arrLength);
            else
            {
                lowerBound = lowerBounds[0];
                res = Array.CreateInstance(itemType, new int[] { arrLength }, lowerBounds);
            }

            int endIndex = lowerBound + arrLength;
            for (int i = lowerBound; i < endIndex; i++) res.SetValue(perItem(itemType, reader, perItemMap), i);

            return res;
        }

        unsafe object DeserializeMultiDimensionalArray(int[] lowerBounds, int rank, Type itemType, ABSaveReader reader, ABSaveMapItem perItemMap)
        {
            int[] lengths = new int[rank];
            for (int i = 0; i < rank; i++)
                lengths[i] = (int)reader.ReadInt32();

            var perItem = CollectionHelpers.GetDeserializeCorrectPerItemOperation(itemType, reader.Settings, perItemMap);

            Array res = lowerBounds == null ? Array.CreateInstance(itemType, lengths) : Array.CreateInstance(itemType, lengths, lowerBounds);
            DeserializeDimension(0, lengths, lowerBounds ?? new int[rank], new DeserializationArrayInfo(res, reader, itemType, perItem, perItemMap));
            return res;
        }

        void DeserializeDimension(int dimension, int[] lengths, int[] currentPos, DeserializationArrayInfo info)
        {
            int oldPos = currentPos[dimension];

            int endIndex = currentPos[dimension] + lengths[dimension];
            int nextDimension = dimension + 1;

            // If this is the deepest we can get, deserialize the items in this dimension.
            if (nextDimension == info.Result.Rank)
                for (; currentPos[dimension] < endIndex; currentPos[dimension]++) info.Result.SetValue(info.PerItem(info.ItemType, info.Reader, info.PerItemMap), currentPos);
            else
                for (; currentPos[dimension] < endIndex; currentPos[dimension]++)
                    DeserializeDimension(nextDimension, lengths, currentPos, info);

            currentPos[dimension] = oldPos;
        }

        unsafe bool TryFastReadArray(int length, ABSaveReader reader, Type itemType, out Array arr)
        {
            switch (Type.GetTypeCode(itemType))
            {
                case TypeCode.Byte:
                    arr = new byte[length];
                    reader.ReadBytes((byte[])arr);

                    return true;
                case TypeCode.SByte:
                    arr = new sbyte[length];
                    fixed (sbyte* arrData = (sbyte[])arr)
                        reader.ReadBytes(new Span<byte>(arrData, length));

                    return true;

                case TypeCode.Char:
                    arr = new char[length];
                    fixed (char* arrData = (char[])arr)
                        reader.FastReadShorts((ushort*)arrData, (uint)length);

                    return true;
                case TypeCode.Int16:
                    arr = new short[length];
                    fixed (short* arrData = (short[])arr)
                        reader.FastReadShorts((ushort*)arrData, (uint)length);

                    return true;
                case TypeCode.UInt16:
                    arr = new ushort[length];
                    fixed (ushort* arrData = (ushort[])arr)
                        reader.FastReadShorts(arrData, (uint)length);

                    return true;
                default:
                    arr = null;
                    return false;
            }
        }

        #endregion

        struct SerializationArrayInfo
        {
            public Type ItemType;
            public ABSaveReader Reader;
            public Action<object, Type, ABSaveReader, ABSaveMapItem> PerItem;
            public ABSaveMapItem PerItemMap;

            public SerializationArrayInfo(Array result, ABSaveReader reader, Type itemType, Action<object, Type, ABSaveReader, ABSaveMapItem> perItem, ABSaveMapItem perItemMap)
            {
                ItemType = itemType;
                Reader = reader;
                PerItem = perItem;
                PerItemMap = perItemMap;
            }
        }

        struct DeserializationArrayInfo
        {
            public Array Result;
            public Type ItemType;
            public ABSaveReader Reader;
            public Func<Type, ABSaveReader, ABSaveMapItem, object> PerItem;
            public ABSaveMapItem PerItemMap;

            public DeserializationArrayInfo(Array result, ABSaveReader reader, Type itemType, Func<Type, ABSaveReader, ABSaveMapItem, object> perItem, ABSaveMapItem perItemMap)
            {
                Result = result;
                ItemType = itemType;
                Reader = reader;
                PerItem = perItem;
                PerItemMap = perItemMap;
            }
        }
    }
}
