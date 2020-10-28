using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Mapping.Generation;
using ABSoftware.ABSave.Mapping.Representation;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Converters
{
    public class ArrayTypeConverter : ABSaveTypeConverter
    {
        public readonly static ArrayTypeConverter Instance = new ArrayTypeConverter();

        private ArrayTypeConverter() { }

        public override bool HasNonExactTypes => true;
        public override Type[] ExactTypes { get; } = new Type[]
        {
            typeof(Array),
            typeof(string[]),
            typeof(int[]),
        };

        #region Serialization

        public override void SerializeData(object obj, Type actualType, IABSaveConverterContext context, ABSaveWriter writer) 
        {
            var asArray = (Array)obj;
            var arrContext = (ArrayConverterContext)context;

            if (arrContext.IsUnknown)
            {
                SerializeUnknown(asArray, actualType, writer);
                return;
            }

            if (asArray.Rank == 1)
            {
                if (arrContext.FastConversion == FastConversionType.None)
                    SerializeSingleDimensionalArray(asArray, arrContext.ElementType, arrContext.PerItem, writer);
                else
                    FastWriteArray(asArray, writer, arrContext.FastConversion);
            }
            else SerializeMultiDimensionalArray(asArray, arrContext.ElementType, arrContext.PerItem, writer);
        }

        // Context will be "unknown" if we had a general "Array" type that said nothing about the array.
        void SerializeUnknown(Array obj, Type actualType, ABSaveWriter writer)
        {
            var elementType = obj.GetType().GetElementType();

            if (obj.Rank == 1)
            {
                var fastWrite = GetFastType(elementType);

                if (fastWrite == FastConversionType.None)
                    SerializeSingleDimensionalArray(obj, elementType, ABSaveMapGenerator.GenerateNonGeneric(writer.Settings, elementType), writer);
                else
                    FastWriteArray(obj, writer, fastWrite);
            }
            else
                SerializeMultiDimensionalArray(obj, elementType, ABSaveMapGenerator.GenerateNonGeneric(writer.Settings, elementType), writer);
        }

        void SerializeSingleDimensionalArray(Array arr, Type elementType, ABSaveMapItem perItem, ABSaveWriter writer)
        {
            int lowerBound = arr.GetLowerBound(0);
            var defaultLowerBound = lowerBound == 0;
            writer.WriteByte(defaultLowerBound ? (byte)0 : (byte)1);

            if (!defaultLowerBound) writer.WriteInt32((uint)lowerBound);

            writer.WriteInt32((uint)arr.Length);

            int endIndex = lowerBound + arr.Length;

            for (int i = lowerBound; i < endIndex; i++) perItem.SerializeData(arr.GetValue(i), elementType, writer);
        }

        void SerializeMultiDimensionalArray(Array arr, Type elementType, ABSaveMapItem perItem, ABSaveWriter writer)
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

            SerializeDimension(0, lengths, lowerBounds, new SerializationArrayInfo(arr, elementType, perItem, writer));
        }

        void SerializeDimension(int dimension, Span<int> lengths, int[] currentPos, SerializationArrayInfo info)
        {
            int oldPos = currentPos[dimension];

            int endIndex = currentPos[dimension] + lengths[dimension];
            int nextDimension = dimension + 1;

            // If this is the deepest we can get, serialize the items in this dimension.
            if (nextDimension == info.Array.Rank)
            {
                for (; currentPos[dimension] < endIndex; currentPos[dimension]++) info.PerItem.SerializeData(info.Array.GetValue(currentPos), info.ElementType, info.Writer);
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
        #endregion

        #region Deserialization

        public override object Deserialize(IABSaveConverterContext context, ABSaveReader reader)
        {
            var firstByte = reader.ReadByte();
            bool hasEqualLengths = (firstByte & 2) > 0;
            bool hasCustomLowerBounds = (firstByte & 1) > 0;

            var arrContext = (ArrayConverterContext)context;
            if (arrContext.IsUnknown) return DeserializeUnknown(firstByte, reader);

            var lowerBounds = hasCustomLowerBounds ? ReadLowerBounds(reader, arrContext.Rank) : null;
            int length = (int)reader.ReadInt32();

            if (arrContext.Rank == 1)
            {
                if (arrContext.FastConversion == FastConversionType.None)
                    return DeserializeSingleDimensionalArray(lowerBounds, arrContext.ElementType, reader);
                else
                    FastReadArray(length, reader, arrContext.FastConversion);
            }
            else DeserializeMultiDimensionalArray(lowerBounds, arrContext.Rank, arrContext.ElementType, arrContext.PerItem, reader);
        }

        private object DeserializeUnknown(byte firstByte, Type elementType, ABSaveReader reader)
        {
            var isMultidimensional = (firstByte & 4) > 0;
            var lowerBounds = ReadLowerBounds(reader, rank);

            if (isMultidimensional)
            {
                int rank = (int)reader.ReadInt32();
                return DeserializeMultiDimensionalArray(lowerBounds, rank, elementType, ABSaveMapGenerator.GenerateNonGeneric(reader.Settings, elementType), reader);
            }
            else return DeserializeSingleDimensionalArray(lowerBounds, elementType, reader);
        }

        public object DeserializeArray(Type elementType, ABSaveReader reader)
        {
            if (asArray.Rank == 1)
            {
                if (arrContext.FastConversion == FastConversionType.None)
                    SerializeSingleDimensionalArray(asArray, arrContext.ElementType, arrContext.PerItem, writer);
                else
                    FastWriteArray(asArray, writer, arrContext.FastConversion);
            }
            else SerializeMultiDimensionalArray(asArray, arrContext.ElementType, arrContext.PerItem, writer);
        }

        object DeserializeSingleDimensionalArray(int[] lowerBounds, Type itemType, ABSaveReader reader)
        {
            var arrLength = (int)reader.ReadInt32();

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

        unsafe object DeserializeMultiDimensionalArray(int[] lowerBounds, int rank, Type itemType, ABSaveMapItem perItem, ABSaveReader reader)
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

        Func<Type, ABSaveReader, ABSaveTypeConverter, ABSaveMapItemOLD, object> GetDeserializePerItemAction(Type itemType, ABSaveSettings settings, out ABSaveTypeConverter converter, ABSaveMapItemOLD mapItem)
        {
            if (mapItem == null)
                return CollectionHelpers.GetDeserializePerItemAction(itemType, settings, out converter);
            else
            {
                converter = null;
                return (itemType, writer, _, map) => map.Deserialize(itemType, writer);
            }
        }

        private static int[] ReadLowerBounds(ABSaveReader reader, int rank)
        {
            int[] lowerBounds = new int[rank];
            for (int i = 0; i < rank; i++)
                lowerBounds[i] = (int)reader.ReadInt32();

            return lowerBounds;
        }

        #endregion

        #region Context Creation

        enum FastConversionType : byte
        {
            None = 0,

            // Matched to "TypeCode"
            Byte = 4,
            SByte = 5,
            Char = 6,
            Short = 7,
            UShort = 8
        }

        class ArrayConverterContext : IABSaveConverterContext
        {
            public static IABSaveConverterContext Unknown = new ArrayConverterContext() { IsUnknown = true };

            public bool IsUnknown;
            public FastConversionType FastConversion;
            public Type ElementType;
            public ABSaveMapItem PerItem;
            public int Rank; // Used for more efficient IL generation
        }

        public override IABSaveConverterContext TryGenerateContext(ABSaveSettings settings, Type type)
        {
            if (!type.IsArray) return null;
            if (type == typeof(Array)) return ArrayConverterContext.Unknown;

            var res = new ArrayConverterContext()
            {
                ElementType = type.GetElementType(),
                Rank = type.GetArrayRank()
            };

            res.FastConversion = res.Rank == 1 ? GetFastType(res.ElementType) : FastConversionType.None;
            res.PerItem = ABSaveMapGenerator.GenerateNonGeneric(settings, res.ElementType);
            return res;
        }

        FastConversionType GetFastType(Type elementType)
        {
            var typeCode = (byte)Type.GetTypeCode(elementType);

            // Map the type code directly onto our "FastConversionType".
            if (typeCode >= 4 && typeCode <= 8) return (FastConversionType)typeCode;
            else return FastConversionType.None;
        }

        #endregion

        #region Primitive Optimization

        unsafe void FastWriteArray(Array arr, ABSaveWriter writer, FastConversionType type)
        {
            switch (type)
            {
                case FastConversionType.Byte:
                    writer.WriteByteArray((byte[])arr, true);
                    break;
                case FastConversionType.SByte:
                    fixed (sbyte* s = (sbyte[])arr)
                        writer.WriteBytes(new Span<byte>(s, arr.Length), true);

                    break;
                case FastConversionType.Char:
                    writer.WriteCharArray((char[])arr);
                    break;
                case FastConversionType.Short:
                    fixed (short* s = (short[])arr)
                    {
                        writer.WriteInt32((uint)arr.Length);
                        writer.FastWriteShorts((ushort*)s, arr.Length);
                    }

                    break;
                case FastConversionType.UShort:
                    fixed (ushort* s = (ushort[])arr)
                    {
                        writer.WriteInt32((uint)arr.Length);
                        writer.FastWriteShorts(s, arr.Length);
                    }

                    break;
                default:
                    throw new Exception("ABSAVE: The context given was invalid.");
            }
        }

        unsafe Array FastReadArray(int length, ABSaveReader reader, FastConversionType type)
        {
            switch (type)
            {
                case FastConversionType.Byte:
                    {
                        var arr = new byte[length];
                        reader.ReadBytes(arr);
                        return arr;
                    }
                case FastConversionType.SByte:
                    {
                        var arr = new sbyte[length];
                        fixed (sbyte* arrData = arr)
                            reader.ReadBytes(new Span<byte>(arrData, length));

                        return arr;
                    }

                case FastConversionType.Char:
                    {
                        var arr = new char[length];
                        fixed (char* arrData = arr)
                            reader.FastReadShorts((ushort*)arrData, (uint)length);

                        return arr;
                    }
                case FastConversionType.Short:
                    {
                        var arr = new short[length];
                        fixed (short* arrData = (short[])arr)
                            reader.FastReadShorts((ushort*)arrData, (uint)length);

                        return arr;
                    }
                case FastConversionType.UShort:
                    {
                        var arr = new ushort[length];
                        fixed (ushort* arrData = (ushort[])arr)
                            reader.FastReadShorts(arrData, (uint)length);

                        return arr;
                    }
                default:
                    throw new Exception("ABSAVE: The context given was invalid.");
            }
        }

        #endregion

        struct SerializationArrayInfo
        {
            public Array Array;
            public Type ElementType;
            public ABSaveMapItem PerItem;
            public ABSaveWriter Writer;

            public SerializationArrayInfo(Array arr, Type elementType, ABSaveMapItem perItem, ABSaveWriter writer)
            {
                Array = arr;
                Writer = writer;
                ElementType = elementType;
                PerItem = perItem;
            }
        }

        struct DeserializationArrayInfo
        {
            public Array Result;
            public Type ItemType;
            public ABSaveReader Reader;
            public ABSaveTypeConverter Converter;
            public ABSaveMapItemOLD PerItemMap;
            public Func<Type, ABSaveReader, ABSaveTypeConverter, ABSaveMapItemOLD, object> PerItem;

            public DeserializationArrayInfo(Array result, Type itemType, ABSaveReader reader, ABSaveTypeConverter converter, ABSaveMapItemOLD perItemMap, Func<Type, ABSaveReader, ABSaveTypeConverter, ABSaveMapItemOLD, object> perItem)
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
