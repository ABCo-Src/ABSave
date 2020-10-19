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
            if (arr.Rank == 1) SerializeSingleDimensionalArray(arr, itemType, writer, map?.PerItem);
            else SerializeMultiDimensionalArray(arr, itemType, writer, map?.PerItem);
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

            int endIndex = lowerBound + arr.Length;

            var perItem = GetSerializePerItemAction(itemType, writer.Settings, out ABSaveTypeConverter converter, perItemMap);
            for (int i = lowerBound; i < endIndex; i++) perItem(arr.GetValue(i), itemType, writer, converter, perItemMap);
        }

        void SerializeMultiDimensionalArray(Array arr, Type itemType, ABSaveWriter writer, ABSaveMapItem perItemMap)
        {
            Span<int> lengths = stackalloc int[arr.Rank];
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

            var perItem = GetSerializePerItemAction(itemType, writer.Settings, out ABSaveTypeConverter converter, perItemMap);
            SerializeDimension(0, lengths, lowerBounds, new SerializationArrayInfo(arr, itemType, writer, converter, perItemMap, perItem));
        }

        void SerializeDimension(int dimension, Span<int> lengths, int[] currentPos, SerializationArrayInfo info)
        {
            int oldPos = currentPos[dimension];

            int endIndex = currentPos[dimension] + lengths[dimension];
            int nextDimension = dimension + 1;

            // If this is the deepest we can get, serialize the items in this dimension.
            if (nextDimension == info.Array.Rank)
            {
                for (; currentPos[dimension] < endIndex; currentPos[dimension]++) info.PerItem(info.Array.GetValue(currentPos), info.ItemType, info.Writer, info.Converter, info.PerItemMap);
            }
            else
                for (; currentPos[dimension] < endIndex; currentPos[dimension]++)
                    SerializeDimension(nextDimension, lengths, currentPos, info);

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

        Action<object, Type, ABSaveWriter, ABSaveTypeConverter, ABSaveMapItem> GetSerializePerItemAction(Type itemType, ABSaveSettings settings, out ABSaveTypeConverter converter, ABSaveMapItem mapItem)
        {
            if (mapItem == null)
                return CollectionHelpers.GetSerializePerItemAction(itemType, settings, out converter);
            else
            {
                converter = null;
                return (item, itemType, writer, _, map) => map.Serialize(item, itemType, writer);
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

            if (isMultidimension) return DeserializeMultiDimensionalArray(lowerBounds, rank, elementType, reader, map?.PerItem); 
            else return DeserializeSingleDimensionalArray(lowerBounds, elementType, reader, map?.PerItem);
        }

        object DeserializeSingleDimensionalArray(int[] lowerBounds, Type itemType, ABSaveReader reader, ABSaveMapItem perItemMap)
        {
            var arrLength = (int)reader.ReadInt32();

            if (TryFastReadArray(arrLength, reader, itemType, out Array arr)) return arr;

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

            var perItem = GetDeserializePerItemAction(itemType, reader.Settings, out ABSaveTypeConverter converter, perItemMap);
            for (int i = lowerBound; i < endIndex; i++) res.SetValue(perItem(itemType, reader, converter, perItemMap), i);

            return res;
        }

        unsafe object DeserializeMultiDimensionalArray(int[] lowerBounds, int rank, Type itemType, ABSaveReader reader, ABSaveMapItem perItemMap)
        {
            int[] lengths = new int[rank];
            for (int i = 0; i < rank; i++)
                lengths[i] = (int)reader.ReadInt32();

            Array res = lowerBounds == null ? Array.CreateInstance(itemType, lengths) : Array.CreateInstance(itemType, lengths, lowerBounds);

            var perItem = GetDeserializePerItemAction(itemType, reader.Settings, out ABSaveTypeConverter converter, perItemMap);
            DeserializeDimension(0, lengths, lowerBounds ?? new int[rank], new DeserializationArrayInfo(res, itemType, reader, converter, perItemMap, perItem));
            return res;
        }

        void DeserializeDimension(int dimension, int[] lengths, int[] currentPos, DeserializationArrayInfo info)
        {
            int oldPos = currentPos[dimension];

            int endIndex = currentPos[dimension] + lengths[dimension];
            int nextDimension = dimension + 1;

            // If this is the deepest we can get, deserialize the items in this dimension.
            if (nextDimension == info.Result.Rank)
                for (; currentPos[dimension] < endIndex; currentPos[dimension]++) info.Result.SetValue(info.PerItem(info.ItemType, info.Reader, info.Converter, info.PerItemMap), currentPos);
            else
                for (; currentPos[dimension] < endIndex; currentPos[dimension]++)
                    DeserializeDimension(nextDimension, lengths, currentPos, info);

            currentPos[dimension] = oldPos;
        }

        Func<Type, ABSaveReader, ABSaveTypeConverter, ABSaveMapItem, object> GetDeserializePerItemAction(Type itemType, ABSaveSettings settings, out ABSaveTypeConverter converter, ABSaveMapItem mapItem)
        {
            if (mapItem == null)
                return CollectionHelpers.GetDeserializePerItemAction(itemType, settings, out converter);
            else
            {
                converter = null;
                return (itemType, writer, _, map) => map.Deserialize(itemType, writer);
            }
        }


        #endregion

        #region Primitive Optimization

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
            public Array Array;
            public Type ItemType;
            public ABSaveWriter Writer;
            public ABSaveTypeConverter Converter;
            public ABSaveMapItem PerItemMap;
            public Action<object, Type, ABSaveWriter, ABSaveTypeConverter, ABSaveMapItem> PerItem;

            public SerializationArrayInfo(Array arr, Type itemType, ABSaveWriter writer, ABSaveTypeConverter converter, ABSaveMapItem perItemMap, Action<object, Type, ABSaveWriter, ABSaveTypeConverter, ABSaveMapItem> perItem)
            {
                Array = arr;
                ItemType = itemType;
                Writer = writer;
                Converter = converter;
                PerItemMap = perItemMap;
                PerItem = perItem;
            }
        }

        struct DeserializationArrayInfo
        {
            public Array Result;
            public Type ItemType;
            public ABSaveReader Reader;
            public ABSaveTypeConverter Converter;
            public ABSaveMapItem PerItemMap;
            public Func<Type, ABSaveReader, ABSaveTypeConverter, ABSaveMapItem, object> PerItem;

            public DeserializationArrayInfo(Array result, Type itemType, ABSaveReader reader, ABSaveTypeConverter converter, ABSaveMapItem perItemMap, Func<Type, ABSaveReader, ABSaveTypeConverter, ABSaveMapItem, object> perItem)
            {
                Result = result;
                ItemType = itemType;
                Reader = reader;
                Converter = converter;
                PerItemMap = perItemMap;
                PerItem = perItem;
            }
        }
    }
}
