﻿using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ABSoftware.ABSave.Converters
{
    public class ArrayConverter : ABSaveConverter
    {
        public static ArrayConverter Instance { get; } = new ArrayConverter();
        private ArrayConverter() { }

        public override bool AlsoConvertsNonExact => true;
        public override bool WritesToHeader => true;
        public override bool ConvertsSubTypes => false;

        // Remember to update in "ABSaveTypeConverter".
        public override Type[] ExactTypes { get; } = new Type[]
        {
            typeof(Array),
            typeof(byte[]),
            typeof(string[]),
            typeof(int[]),
        };

        #region Serialization

        public override void Serialize(object obj, Type actualType, IABSaveConverterContext context, ref BitTarget header)
        {
            var arrContext = (Context)context;
            Serialize((Array)obj, actualType, ref arrContext.Info, ref header);
        }

        void Serialize(Array arr, Type actualType, ref ArrayTypeInfo context, ref BitTarget header)
        {
            var len = arr.Length;

            switch (context.Type)
            {
                case ArrayType.SZArrayFast:
                    {
                        SerializeFast(arr, context.FastConversion, ref header);

                        break;
                    }
                case ArrayType.SZArrayManual:
                    {
                        header.Serializer.WriteCompressed((uint)len, ref header);
                        for (int i = 0; i < len; i++) header.Serializer.SerializeItem(arr.GetValue(i), context.PerItem);

                        break;
                    }

                // Extremely rare to not be in an "Array" (unknown), but may as well support it.
                case ArrayType.SNZArray:
                    {
                        header.Serializer.WriteCompressed((uint)len, ref header);

                        int i = arr.GetLowerBound(0);
                        header.Serializer.WriteCompressed((uint)i);

                        int end = i + len;
                        for (; i < end; i++) header.Serializer.SerializeItem(arr.GetValue(i), context.PerItem);

                        break;
                    }
                case ArrayType.MultiDimensional:
                    {
                        // Get information about the array.
                        var mdContext = GetMultiDimensionInfo(arr, ref context, header.Serializer, out int[] lowerBounds);
                        int firstLength = arr.GetLength(0);

                        header.WriteBitWith(mdContext.CustomLowerBounds);
                        header.Serializer.WriteCompressed((uint)firstLength, ref header);

                        SerializeMultiDimensionalArrayData(arr, ref context, ref mdContext, firstLength, lowerBounds, header.Serializer);

                        break;
                    }

                // Unknown
                default:
                    SerializeUnknown(arr, actualType, ref header);
                    break;
            }
        }

        private void SerializeUnknown(Array arr, Type actualType, ref BitTarget typeHeader)
        {
            var context = new ArrayTypeInfo();
            PopulateTypeInfo(ref context, typeHeader.Serializer.Map, actualType);

            typeHeader.Serializer.WriteClosedType(context.ElementType, ref typeHeader);

            var header = new BitTarget(typeHeader.Serializer);

            // Write the unknown information.
            switch (context.Type)
            {
                case ArrayType.SZArrayFast:

                    header.WriteBitOff();
                    header.WriteBitOff();
                    SerializeFast(arr, context.FastConversion, ref header);

                    break;
                case ArrayType.SZArrayManual:

                    header.WriteBitOff();
                    header.WriteBitOff();
                    header.Serializer.WriteCompressed((uint)arr.Length, ref header);

                    for (int i = 0; i < arr.Length; i++) header.Serializer.SerializeItem(arr.GetValue(i), context.PerItem);

                    break;
                case ArrayType.SNZArray:

                    // Write the header
                    header.WriteBitOff();
                    header.WriteBitOn();
                    header.Serializer.WriteCompressed((uint)arr.Length, ref header);

                    int j = arr.GetLowerBound(0);
                    header.Serializer.WriteCompressed((ulong)j);

                    int endIndex = j + arr.Length;
                    for (; j < endIndex; j++) header.Serializer.SerializeItem(arr.GetValue(j), context.PerItem);

                    break;
                case ArrayType.MultiDimensional:

                    // Get information
                    var mdContext = GetMultiDimensionInfo(arr, ref context, header.Serializer, out int[] lowerBounds);
                    int firstLength = arr.GetLength(0);

                    header.WriteBitOn();
                    header.WriteBitWith(mdContext.CustomLowerBounds);
                    header.WriteInteger(context.Rank, 5);
                    header.Apply();

                    header.Serializer.WriteCompressed((uint)firstLength);                   

                    SerializeMultiDimensionalArrayData(arr, ref context, ref mdContext, firstLength, lowerBounds, header.Serializer);

                    break;
                case ArrayType.Unknown:
                    throw new Exception("ABSAVE: An array could not serialized.");
            }
        }

        // Serializes the non-first lengths, lower bounds, and items.
        void SerializeMultiDimensionalArrayData(Array arr, ref ArrayTypeInfo context, ref MDSerializeArrayInfo mdContext, int firstLength, int[] lowerBounds, ABSaveSerializer serializer)
        {
            Span<int> lengths = stackalloc int[context.Rank];
            lengths[0] = firstLength;

            // Load and write the lengths.
            for (int i = 1; i < lengths.Length; i++)
            {
                lengths[i] = arr.GetLength(i);
                serializer.WriteCompressed((ulong)lengths[i]);
            }

            // Write the lower bounds
            if (mdContext.CustomLowerBounds)
                for (int i = 0; i < lengths.Length; i++)
                    serializer.WriteCompressed((ulong)lowerBounds[i]);

            SerializeDimension(0, lengths, lowerBounds, ref mdContext, ref context);
        }

        void SerializeDimension(int dimension, ReadOnlySpan<int> lengths, int[] currentPos, ref MDSerializeArrayInfo mdInfo, ref ArrayTypeInfo info)
        {
            // The currentPos gets reset back to what it was once we're done serializing the current dimension.
            int originalPos = currentPos[dimension];

            int endIndex = currentPos[dimension] + lengths[dimension];

            int nextDimension = dimension + 1;

            // Deepest dimension
            if (nextDimension == mdInfo.Array.Rank)
                for (; currentPos[dimension] < endIndex; currentPos[dimension]++) 
                    mdInfo.Serializer.SerializeItem(mdInfo.Array.GetValue(currentPos), info.PerItem);

            // Outer dimension
            else
                for (; currentPos[dimension] < endIndex; currentPos[dimension]++)
                    SerializeDimension(nextDimension, lengths, currentPos, ref mdInfo, ref info);

            currentPos[dimension] = originalPos;
        }

        MDSerializeArrayInfo GetMultiDimensionInfo(Array arr, ref ArrayTypeInfo context, ABSaveSerializer serializer, out int[] lowerBounds)
        {
            lowerBounds = new int[context.Rank];

            bool customLowerBounds = false;
            for (int i = 0; i < context.Rank; i++)
            {
                int current = arr.GetLowerBound(i);
                lowerBounds[i] = current;

                if (current != 0)
                    customLowerBounds = true;
            }

            return new MDSerializeArrayInfo(arr, serializer, customLowerBounds);
        }

        #endregion

        #region Deserialization

        public override object Deserialize(Type actualType, IABSaveConverterContext context, ref BitSource header)
        {
            var arrContext = (Context)context;
            return Deserialize(ref arrContext.Info, ref header);
        }

        object Deserialize(ref ArrayTypeInfo context, ref BitSource header)
        {
            switch (context.Type)
            {
                case ArrayType.SZArrayFast:
                    return DeserializeFast(context.FastConversion, ref header);
                case ArrayType.SZArrayManual:
                    {
                        int arrSize = (int)header.Deserializer.ReadCompressedInt(ref header);
                        Array arr = Array.CreateInstance(context.ElementType, arrSize);

                        for (int i = 0; i < arrSize; i++) arr.SetValue(header.Deserializer.DeserializeItem(context.PerItem), i);
                        return arr;
                    }
                case ArrayType.SNZArray:
                    {
                        int arrSize = (int)header.Deserializer.ReadCompressedInt(ref header);
                        int i = (int)header.Deserializer.ReadCompressedInt(ref header);

                        var arr = Array.CreateInstance(context.ElementType, new int[] { arrSize }, new int[] { i });

                        int end = i + arrSize;
                        for (; i < end; i++) arr.SetValue(header.Deserializer.DeserializeItem(context.PerItem), i);
                        return arr;
                    }
                case ArrayType.MultiDimensional:
                    {
                        bool hasCustomLowerBounds = header.ReadBit();
                        int arrSize = (int)header.Deserializer.ReadCompressedInt(ref header);

                        return DeserializeMultiDimensionalArray(in context, hasCustomLowerBounds, arrSize, header.Deserializer);
                    }

                // Unknown
                default:
                    return DeserializeUnknown(ref header);
            }
        }

        private object DeserializeUnknown(ref BitSource typeHeader)
        {
            // Get type information.
            Type elementType = typeHeader.Deserializer.ReadClosedType(ref typeHeader);
            var perItem = typeHeader.Deserializer.GetRuntimeMapItem(elementType);

            // Read the header information
            var header = new BitSource(typeHeader.Deserializer);
            bool isMultiDimensional = header.ReadBit();
            bool hasCustomLowerBounds = header.ReadBit();

            // Multi-dimensional
            if (isMultiDimensional)
            {
                int rank = header.ReadInteger(5);

                int size = (int)header.Deserializer.ReadCompressedInt();
                var context = new ArrayTypeInfo((byte)rank, elementType, perItem);
                return DeserializeMultiDimensionalArray(in context, hasCustomLowerBounds, size, header.Deserializer);
            }

            // Single-dimensional
            else
            {
                // SNZArray
                if (hasCustomLowerBounds)
                {
                    int size = (int)header.Deserializer.ReadCompressedInt(ref header);

                    int i = (int)header.Deserializer.ReadCompressedInt();
                    var arr = Array.CreateInstance(elementType, new int[] { size }, new int[] { i });

                    int end = i + size;
                    for (; i < end; i++) arr.SetValue(header.Deserializer.DeserializeItem(perItem), i);
                    return arr;
                }

                // SZArray
                else
                {
                    // Try to fast convert
                    var fastType = GetFastType(elementType);
                    if (fastType != FastConversionType.None) return DeserializeFast(fastType, ref header);

                    int size = (int)header.Deserializer.ReadCompressedInt(ref header);
                    var arr = Array.CreateInstance(elementType, size);

                    for (int i = 0; i < size; i++) arr.SetValue(header.Deserializer.DeserializeItem(perItem), i);
                    return arr;
                }
            }
        }

        unsafe object DeserializeMultiDimensionalArray(in ArrayTypeInfo context, bool hasCustomLowerBounds, int firstLength, ABSaveDeserializer deserializer)
        {
            // Read the lengths.
            int[] lengths = new int[context.Rank];
            lengths[0] = firstLength;

            for (int i = 1; i < context.Rank; i++)
                lengths[i] = (int)deserializer.ReadCompressedInt();

            // Read the lower bounds.
            int[] lowerBounds = new int[context.Rank];
            if (hasCustomLowerBounds)
            {
                for (int i = 0; i < context.Rank; i++)
                    lowerBounds[i] = (int)deserializer.ReadCompressedInt();
            }

            // Create the array, and deserialize.
            Array res = hasCustomLowerBounds ? Array.CreateInstance(context.ElementType, lengths, lowerBounds) : Array.CreateInstance(context.ElementType, lengths);

            var mdContext = new MDDeserializeArrayInfo(res, deserializer);
            DeserializeDimension(0, lengths, lowerBounds, in context, in mdContext);

            return res;
        }

        void DeserializeDimension(int dimension, int[] lengths, int[] currentPos, in ArrayTypeInfo context, in MDDeserializeArrayInfo mdContext)
        {
            // The currentPos gets reset back to what it was once we're done deserializing the current dimension.
            int oldPos = currentPos[dimension];

            int endIndex = currentPos[dimension] + lengths[dimension];
            int nextDimension = dimension + 1;

            // Deepest dimension
            if (nextDimension == mdContext.Result.Rank)
                for (; currentPos[dimension] < endIndex; currentPos[dimension]++) 
                    mdContext.Result.SetValue(mdContext.Deserializer.DeserializeItem(context.PerItem), currentPos);

            // Outer dimension
            else
                for (; currentPos[dimension] < endIndex; currentPos[dimension]++)
                    DeserializeDimension(nextDimension, lengths, currentPos, in context, in mdContext);

            currentPos[dimension] = oldPos;
        }

        #endregion

        #region Context Creation

        public override IABSaveConverterContext TryGenerateContext(ABSaveMap map, Type type)
        {
            if (type == typeof(Array)) return Context.Unknown;
            if (!type.IsArray) return null;

            var res = new Context();
            PopulateTypeInfo(ref res.Info, map, type);
            return res;
        }

        void PopulateTypeInfo(ref ArrayTypeInfo info, ABSaveMap map, Type type)
        {
            var rank = type.GetArrayRank();

            info.ElementType = type.GetElementType();

            if (type.IsSZArray)
            {
                info.FastConversion = GetFastType(info.ElementType);
                info.Type = info.FastConversion == FastConversionType.None ? ArrayType.SZArrayManual : ArrayType.SZArrayFast;
            }
            else if (rank == 1)
            {
                info.Rank = 1;
                info.Type = ArrayType.SNZArray;
            }
            else
            {
                if (rank == 32) throw new Exception("ABSave does not support arrays with exactly 32 dimensions, only below.");
                info.Rank = (byte)rank;
                info.Type = ArrayType.MultiDimensional;
            }

            info.PerItem = map.GetMaptimeSubItem(info.ElementType);
        }

        FastConversionType GetFastType(Type elementType) => Type.GetTypeCode(elementType) switch
        {
            TypeCode.Byte => FastConversionType.Byte,
            TypeCode.SByte => FastConversionType.SByte,
            TypeCode.Int16 => FastConversionType.Short,
            TypeCode.UInt16 => FastConversionType.UShort,
            TypeCode.Char => FastConversionType.Char,
            _ => FastConversionType.None
        };

        #endregion

        #region Primitive Optimization

        unsafe void SerializeFast(Array arr, FastConversionType type, ref BitTarget header)
        {
            if (type == FastConversionType.Char) TextConverter.Instance.SerializeCharArray((char[])arr, ref header);
            header.Serializer.WriteCompressed((uint)arr.Length, ref header);

            switch (type)
            {
                case FastConversionType.Byte:
                    header.Serializer.WriteByteArray((byte[])arr);
                    break;
                case FastConversionType.SByte:
                    var data = ((sbyte[])arr).AsSpan();
                    header.Serializer.WriteBytes(MemoryMarshal.Cast<sbyte, byte>(data));

                    break;
                case FastConversionType.Short:
                    header.Serializer.FastWriteShorts(((short[])arr).AsSpan());
                    break;
                case FastConversionType.UShort:
                    var shortData = ((ushort[])arr).AsSpan();
                    header.Serializer.FastWriteShorts(MemoryMarshal.Cast<ushort, short>(shortData));
                    break;
                default:
                    throw new Exception("ABSAVE: The context given was invalid.");
            }
        }

        unsafe Array DeserializeFast(FastConversionType type, ref BitSource header)
        {
            if (type == FastConversionType.Char) return TextConverter.Instance.DeserializeCharArray(ref header);

            int length = (int)header.Deserializer.ReadCompressedInt(ref header);
            switch (type)
            {
                case FastConversionType.Byte:
                    {  
                        var arr = new byte[length];
                        header.Deserializer.ReadBytes(arr);
                        return arr;
                    }
                case FastConversionType.SByte:
                    {
                        var arr = new sbyte[length];
                        fixed (sbyte* arrData = arr)
                            header.Deserializer.ReadBytes(new Span<byte>(arrData, length));

                        return arr;
                    }
                case FastConversionType.Short:
                    {
                        var arr = new short[length];
                        header.Deserializer.FastReadShorts(arr.AsSpan());

                        return arr;
                    }
                case FastConversionType.UShort:
                    {
                        var arr = new ushort[length];
                        header.Deserializer.FastReadShorts(MemoryMarshal.Cast<ushort, short>(arr.AsSpan()));

                        return arr;
                    }
                default:
                    throw new Exception("ABSAVE: The context given was invalid.");
            }
        }

        #endregion

        [StructLayout(LayoutKind.Auto)]
        readonly struct MDSerializeArrayInfo
        {
            public readonly Array Array;
            public readonly ABSaveSerializer Serializer;
            public readonly bool CustomLowerBounds;

            public MDSerializeArrayInfo(Array arr, ABSaveSerializer serializer, bool zeroLowerBounds)
            {
                Array = arr;
                Serializer = serializer;
                CustomLowerBounds = zeroLowerBounds;
            }
        }

        [StructLayout(LayoutKind.Auto)]
        readonly struct MDDeserializeArrayInfo
        {
            public readonly Array Result;
            public readonly ABSaveDeserializer Deserializer;

            public MDDeserializeArrayInfo(Array result, ABSaveDeserializer deserializer)
            {
                Result = result;
                Deserializer = deserializer;
            }
        }

        public enum ArrayType : byte
        {
            Unknown = 0,

            // A single-dimensional, zero-based array, where each item has to be manually converted via the map.
            SZArrayManual = 1,

            // A single-dimensional, zero-based array, where each item can be quickly binary converted.
            SZArrayFast = 2,

            // A single-dimensional, non-zero-based array.
            SNZArray = 3,

            // A multi-dimensional (non-zero-based) array.
            MultiDimensional = 4
        }

        public enum FastConversionType : byte
        {
            None,
            Byte,
            SByte,
            Char,
            Short,
            UShort
        }

        class Context : IABSaveConverterContext
        {
            public static Context Unknown = new Context { Info = new ArrayTypeInfo() { Type = ArrayType.Unknown } };

            public ArrayTypeInfo Info = new ArrayTypeInfo();
        }

        [StructLayout(LayoutKind.Auto)]
        struct ArrayTypeInfo
        {
            public ArrayType Type;
            public byte Rank;
            public FastConversionType FastConversion;
            public Type ElementType;
            public MapItem PerItem;

            // For multi-dimensional arrays.
            public ArrayTypeInfo(byte rank, Type elementType, MapItem perItem)
            {
                Type = ArrayType.MultiDimensional;
                FastConversion = FastConversionType.None;

                Rank = rank;
                ElementType = elementType;
                PerItem = perItem;
            }
        }
    }
}