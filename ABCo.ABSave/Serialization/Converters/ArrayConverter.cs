using ABCo.ABSave.Serialization.Reading;
using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Mapping.Generation.Converters;
using ABCo.ABSave.Serialization.Writing;
using System;
using System.Runtime.InteropServices;

namespace ABCo.ABSave.Serialization.Converters
{
    [Select(typeof(Array))]
    [Select(typeof(byte[]), typeof(byte))]
    [Select(typeof(string[]), typeof(string))]
    [Select(typeof(int[]), typeof(int))]
    [SelectOtherWithCheckType]
    public class ArrayConverter : Converter
    {
        ArrayTypeInfo _info;

        public override bool CheckType(CheckTypeInfo info)
        {
            if (info.Type.IsArray) return true;

            if (info.Type == typeof(Array))
            {
                _info.Type = ArrayType.Unknown;
                return true;
            }

            return false;
        }

        public override uint Initialize(InitializeInfo info)
        {
            if (_info.Type != ArrayType.None) return 0;

            Type? elemType = info.Type.GetElementType();
            PopulateTypeInfo(ref _info, info.GetMap(elemType!), info.Type);
            return 0;
        }

        static void PopulateTypeInfo(ref ArrayTypeInfo info, MapItemInfo itemInfo, Type type)
        {
            int rank = type.GetArrayRank();
            info.ElementType = itemInfo.GetItemType();
            info.PerItem = itemInfo;

#if NETSTANDARD2_0
            if (IsSZArrayStandard2(ref info, type))
#else
            if (type.IsSZArray)
#endif
            {
                info.FastItemTypeCode = Type.GetTypeCode(info.ElementType);
                info.Type = 
                    info.FastItemTypeCode >= TypeCode.SByte && info.FastItemTypeCode <= TypeCode.Double
                    ? ArrayType.SZArrayFast : ArrayType.SZArrayManual;

                // char[]s should go to the TextConverter, not here!
                if (info.FastItemTypeCode == TypeCode.Char) throw new Exception("char[] went to ArrayConverter and not TextConverter");

                //info.Type = info.FastConversion == FastConversionType.None ? ArrayType.SZArrayManual : ArrayType.SZArrayFast;
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
        }

#region Serialization

        public override void Serialize(in SerializeInfo info) =>
            Serialize((Array)info.Instance, info.ActualType, info.Serializer);

        void Serialize(Array arr, Type actualType, ABSaveSerializer serializer)
        {
            int len = arr.Length;

            switch (_info.Type)
            {
                case ArrayType.SZArrayFast:
                    {
                        SerializeFast(arr, serializer);

                        break;
                    }
                case ArrayType.SZArrayManual:
                    {
                        serializer.WriteCompressedInt((uint)len);
                        for (int i = 0; i < len; i++) serializer.WriteItem(arr.GetValue(i), _info.PerItem);

                        break;
                    }

                // Extremely rare to not be in an "Array" (unknown), but may as well support it.
                case ArrayType.SNZArray:
                    {
                        serializer.WriteCompressedInt((uint)len);

                        int i = arr.GetLowerBound(0);
                        serializer.WriteCompressedInt((uint)i);

                        int end = i + len;
                        for (; i < end; i++) serializer.WriteItem(arr.GetValue(i), _info.PerItem);

                        break;
                    }
                case ArrayType.MultiDimensional:
                    {
                        // Get information about the array.
                        MDSerializeArrayInfo mdContext = GetMultiDimensionInfo(arr, ref _info, serializer, out int[] lowerBounds);
                        int firstLength = arr.GetLength(0);

                        serializer.WriteBitWith(mdContext.CustomLowerBounds);
                        serializer.WriteCompressedInt((uint)firstLength);

                        SerializeMultiDimensionalArrayData(arr, ref _info, ref mdContext, firstLength, lowerBounds);

                        break;
                    }

                default:
                    throw new Exception("Unknown arrays ('Array') are not currently supported, you can try defining your own converter if you'd like.");

                    // Unknown
                    //default:
                    //    SerializeUnknown(arr, actualType, header);
                    //    break;
            }
        }

        // Serializes the non-first lengths, lower bounds, and items.
        void SerializeMultiDimensionalArrayData(Array arr, ref ArrayTypeInfo context, ref MDSerializeArrayInfo mdContext, int firstLength, int[] lowerBounds)
        {
            Span<int> lengths = stackalloc int[context.Rank];
            lengths[0] = firstLength;

            // Load and write the lengths.
            for (int i = 1; i < lengths.Length; i++)
            {
                lengths[i] = arr.GetLength(i);
                mdContext.Serializer.WriteCompressedLong((ulong)lengths[i]);
            }

            // Write the lower bounds
            if (mdContext.CustomLowerBounds)
                for (int i = 0; i < lengths.Length; i++)
                    mdContext.Serializer.WriteCompressedLong((ulong)lowerBounds[i]);

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
                    mdInfo.Serializer.WriteItem(mdInfo.Array.GetValue(currentPos), info.PerItem);

            // Outer dimension
            else
                for (; currentPos[dimension] < endIndex; currentPos[dimension]++)
                    SerializeDimension(nextDimension, lengths, currentPos, ref mdInfo, ref info);

            currentPos[dimension] = originalPos;
        }

        static MDSerializeArrayInfo GetMultiDimensionInfo(Array arr, ref ArrayTypeInfo context, ABSaveSerializer serializer, out int[] lowerBounds)
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

        public override object Deserialize(in DeserializeInfo info) => Deserialize(info.Deserializer);

        object Deserialize(ABSaveDeserializer deserializer)
        {
            switch (_info.Type)
            {
                case ArrayType.SZArrayFast:
                    return DeserializeFast(deserializer);
                case ArrayType.SZArrayManual:
                    {
                        int arrSize = (int)deserializer.ReadCompressedInt();
                        var arr = Array.CreateInstance(_info.ElementType, arrSize);

                        for (int i = 0; i < arrSize; i++) arr.SetValue(deserializer.ReadItem(_info.PerItem), i);
                        return arr;
                    }
                case ArrayType.SNZArray:
                    {
                        int arrSize = (int)deserializer.ReadCompressedInt();
                        int i = (int)deserializer.ReadCompressedInt();

                        var arr = Array.CreateInstance(_info.ElementType, new int[] { arrSize }, new int[] { i });

                        int end = i + arrSize;
                        for (; i < end; i++) arr.SetValue(deserializer.ReadItem(_info.PerItem), i);
                        return arr;
                    }
                case ArrayType.MultiDimensional:
                    {
                        bool hasCustomLowerBounds = deserializer.ReadBit();
                        int arrSize = (int)deserializer.ReadCompressedInt();

                        return DeserializeMultiDimensionalArray(in _info, hasCustomLowerBounds, arrSize, deserializer);
                    }

                // Unknown
                default:
                    throw new Exception("Unknown arrays ('Array') are not currently supported, you can try defining your own converter if you'd like.");
            }
        }

        unsafe object DeserializeMultiDimensionalArray(in ArrayTypeInfo context, bool hasCustomLowerBounds, int firstLength, ABSaveDeserializer header)
        {
            // Read the lengths.
            int[] lengths = new int[context.Rank];
            lengths[0] = firstLength;

            for (int i = 1; i < context.Rank; i++)
                lengths[i] = (int)header.ReadCompressedInt();

            // Read the lower bounds.
            int[] lowerBounds = new int[context.Rank];
            if (hasCustomLowerBounds)
            {
                for (int i = 0; i < context.Rank; i++)
                    lowerBounds[i] = (int)header.ReadCompressedInt();
            }

            // Create the array, and deserialize.
            Array res = hasCustomLowerBounds ? Array.CreateInstance(context.ElementType, lengths, lowerBounds) : Array.CreateInstance(context.ElementType, lengths);

            var mdContext = new MDDeserializeArrayInfo(res, header);
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
                    mdContext.Result.SetValue(mdContext.Deserializer.ReadItem(context.PerItem), currentPos);

            // Outer dimension
            else
                for (; currentPos[dimension] < endIndex; currentPos[dimension]++)
                    DeserializeDimension(nextDimension, lengths, currentPos, in context, in mdContext);

            currentPos[dimension] = oldPos;
        }

#endregion

        #region Primitive Optimization

        unsafe void SerializeFast(Array arr, ABSaveSerializer serializer)
        {
            // Length
            serializer.WriteCompressedInt((uint)arr.Length);

            // Item Version Info (if necessary)
            serializer.WriteVersionInfo(_info.PerItem.Converter);

            // Data
            switch (_info.FastItemTypeCode)
            {
                case TypeCode.Byte:
                    serializer.WriteRawBytes((byte[])arr);
                    break;
                case TypeCode.SByte:
                    serializer.WriteRawBytes(MemoryMarshal.Cast<sbyte, byte>((sbyte[])arr));
                    break;

                // If we're trying to compress primitives for Int16 and UInt16, fallback to the less specialized SerializeBytesFast path.
                case TypeCode.Int16:
                    if (serializer.State.Settings.CompressPrimitives)
                        SerializeBytesFast((Span<short>)(short[])arr, serializer);
                    else
                        serializer.FastWriteShorts((Span<short>)(short[])arr);

                    break;
                case TypeCode.UInt16:
                    if (serializer.State.Settings.CompressPrimitives)
                        SerializeBytesFast((Span<ushort>)(ushort[])arr, serializer);
                    else
                        serializer.FastWriteShorts(MemoryMarshal.Cast<ushort, short>((ushort[])arr));

                    break;
                case TypeCode.Int32:
                    SerializeBytesFast((Span<int>)(int[])arr, serializer);
                    break;
                case TypeCode.UInt32:
                    SerializeBytesFast((Span<uint>)(uint[])arr, serializer);
                    break;
                case TypeCode.Int64:
                    SerializeBytesFast((Span<long>)(long[])arr, serializer);
                    break;
                case TypeCode.UInt64:
                    SerializeBytesFast((Span<ulong>)(ulong[])arr, serializer);
                    break;
                case TypeCode.Single:
                    SerializeBytesFast((Span<float>)(float[])arr, serializer);
                    break;
                case TypeCode.Double:
                    SerializeBytesFast((Span<double>)(double[])arr, serializer);
                    break;
            }
        }

        unsafe void SerializeBytesFast<T>(Span<T> arr, ABSaveSerializer serializer) where T : struct
        {
            // For now, we don't support these cases. They could be optimized in the future, however.
            if (serializer.State.Settings.CompressPrimitives || serializer.State.ShouldReverseEndian)
            {
                // Manually serialize each item.
                for (int i = 0; i < arr.Length; i++)
                    serializer.WriteExactNonNullItem(arr[i], _info.PerItem);
            }
            else
                serializer.WriteRawBytes(MemoryMarshal.Cast<T, byte>(arr));            

        }

        unsafe Array DeserializeFast(ABSaveDeserializer deserializer)
        {
            // Length
            int len = (int)deserializer.ReadCompressedInt();

            // Item Version Info (if necessary)
            deserializer.ReadVersionInfo(_info.PerItem.Converter);

            // Data
            switch (_info.FastItemTypeCode)
            {
                case TypeCode.Byte:
                    {
                        byte[] arr = new byte[len];
                        deserializer.ReadBytes(arr);
                        return arr;
                    }
                case TypeCode.SByte:
                    {
                        sbyte[] arr = new sbyte[len];
                        deserializer.ReadBytes(MemoryMarshal.Cast<sbyte, byte>(arr));
                        return arr;
                    }
                // If we're trying to compress primitives for Int16 and UInt16, fallback to the less specialized SerializeBytesFast path.
                case TypeCode.Int16:
                    if (deserializer.State.Settings.CompressPrimitives)
                        return DeserializeBytesFast<short>(len, deserializer);
                    {
                        short[] arr = new short[len];
                        deserializer.FastReadShorts(arr);
                        return arr;
                    }
                case TypeCode.UInt16:
                    if (deserializer.State.Settings.CompressPrimitives)
                        return DeserializeBytesFast<ushort>(len, deserializer);
                    {
                        ushort[] arr = new ushort[len];
                        deserializer.FastReadShorts(MemoryMarshal.Cast<ushort, short>(arr));
                        return arr;
                    }
                case TypeCode.Int32:
                    return DeserializeBytesFast<int>(len, deserializer);
                case TypeCode.UInt32:
                    return DeserializeBytesFast<uint>(len, deserializer);
                case TypeCode.Int64:
                    return DeserializeBytesFast<long>(len, deserializer);
                case TypeCode.UInt64:
                    return DeserializeBytesFast<ulong>(len, deserializer);
                case TypeCode.Single:
                    return DeserializeBytesFast<float>(len, deserializer);
                case TypeCode.Double:
                    return DeserializeBytesFast<double>(len, deserializer);
                default:
                    throw new Exception("Invalid TypeCode in fast array converter!");
            }
        }

        unsafe Array DeserializeBytesFast<T>(int len, ABSaveDeserializer deserializer) where T : struct
        {
            T[] res = new T[len];

            // For now, we don't support these cases. They could be optimized in the future, however.
            if (deserializer.State.Settings.CompressPrimitives || deserializer.State.ShouldReverseEndian)
            {
                // Manually deserialize each item.
                for (int i = 0; i < res.Length; i++)
                    res[i] = (T)deserializer.ReadExactNonNullItem(_info.PerItem);
            }
            else
                deserializer.ReadBytes(MemoryMarshal.Cast<T, byte>(res));

            return res;
        }

        //static unsafe void SerializeFast(Array arr, FastConversionType type, ABSaveSerializer header)
        //{
        //    // TODO: Remove tight coupling with TextConverter.
        //    if (type == FastConversionType.Char) TextConverter.SerializeCharArray((char[])arr, ref header);

            //    header.Serializer.WriteCompressed((uint)arr.Length, ref header);

            //    switch (type)
            //    {
            //        case FastConversionType.Byte:
            //            header.Serializer.WriteByteArray((byte[])arr);
            //            break;
            //        case FastConversionType.SByte:
            //            var data = ((sbyte[])arr).AsSpan();
            //            header.Serializer.WriteBytes(MemoryMarshal.Cast<sbyte, byte>(data));

            //            break;
            //        case FastConversionType.Short:
            //            header.Serializer.FastWriteShorts(((short[])arr).AsSpan());
            //            break;
            //        case FastConversionType.UShort:
            //            var shortData = ((ushort[])arr).AsSpan();
            //            header.Serializer.FastWriteShorts(MemoryMarshal.Cast<ushort, short>(shortData));
            //            break;
            //        default:
            //            throw new Exception("ABSAVE: The context given was invalid.");
            //    }
            //}

            //unsafe static Array DeserializeFast(FastConversionType type, ABSaveDeserializer header)
            //{
            //    // TODO: Remove tight coupling with TextConverter.
            //    if (type == FastConversionType.Char) return TextConverter.DeserializeCharArray(ref header);

            //    int length = (int)header.Deserializer.ReadCompressedInt(ref header);
            //    switch (type)
            //    {
            //        case FastConversionType.Byte:
            //            {  
            //                var arr = new byte[length];
            //                header.Deserializer.ReadBytes(arr);
            //                return arr;
            //            }
            //        case FastConversionType.SByte:
            //            {
            //                var arr = new sbyte[length];
            //                fixed (sbyte* arrData = arr)
            //                    header.Deserializer.ReadBytes(new Span<byte>(arrData, length));

            //                return arr;
            //            }
            //        case FastConversionType.Short:
            //            {
            //                var arr = new short[length];
            //                header.Deserializer.FastReadShorts(arr.AsSpan());

            //                return arr;
            //            }
            //        case FastConversionType.UShort:
            //            {
            //                var arr = new ushort[length];
            //                header.Deserializer.FastReadShorts(MemoryMarshal.Cast<ushort, short>(arr.AsSpan()));

            //                return arr;
            //            }
            //        default:
            //            throw new Exception("ABSAVE: The context given was invalid.");
            //    }
            //}

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
            None,

            Unknown,

            // A single-dimensional, zero-based array, where each item has to be manually converted via the map.
            SZArrayManual,

            // A single-dimensional, zero-based array, where each item can be quickly binary converted.
            SZArrayFast,

            // A single-dimensional, non-zero-based array.
            SNZArray,

            // A multi-dimensional (non-zero-based) array.
            MultiDimensional
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

        [StructLayout(LayoutKind.Auto)]
        struct ArrayTypeInfo
        {
            public ArrayType Type;
            public TypeCode FastItemTypeCode;
            public byte Rank;
            public Type ElementType;
            public MapItemInfo PerItem;
        }

#if NETSTANDARD2_0
        static bool IsSZArrayStandard2(ref ArrayTypeInfo info, Type type)
        {
            // This is the best way (sadly) of determining for certain if the type is an SZArray or not on Standard 2.0.
            Type szVersion = info.ElementType.MakeArrayType();
            return type == szVersion;
        }
#endif

        // Internal for testing. I know, this is a little poor, but it is what it is!
        internal bool IsSerializingFast => _info.Type == ArrayType.SZArrayFast;
    }
}
