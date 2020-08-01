using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization;
using ABSoftware.ABSave.Serialization.Writer;
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
    public class CollectionTypeConverter : ABSaveTypeConverter
    {
        public readonly static CollectionTypeConverter Instance = new CollectionTypeConverter();
        private CollectionTypeConverter() { }

        static TypeInformation ObjectTypeInfo = new TypeInformation(null, TypeCode.Empty, typeof(object), TypeCode.Object);

        public override bool HasExactType => false;
        public override bool CheckCanConvertType(TypeInformation typeInfo) => typeInfo.ActualType.IsArray || ABSaveUtils.HasInterface(typeInfo.ActualType, typeof(IEnumerable));

        #region Serialization

        public override void Serialize(object obj, TypeInformation typeInfo, ABSaveWriter writer)
        {
            if (typeInfo.ActualType.IsArray) SerializeArray((Array)obj, typeInfo, writer);
            else 
            {
                var interfaces = typeInfo.ActualType.GetInterfaces();

                if (TryGetIEnumerableGenericArgument(interfaces, out Type specifiedItem)) 
                    SerializeGeneric((dynamic)obj, interfaces, specifiedItem, writer);
                else 
                    SerializeNonGeneric((IEnumerable)obj, writer);
            }
        }

        void SerializeArray(Array arr, TypeInformation typeInfo, ABSaveWriter writer)
        {
            var itemType = typeInfo.ActualType.GetElementType();
            var arrRank = arr.Rank;

            if (arrRank == 1)
                SerializeSingleDimensionalArray(arr, itemType, writer);
            else
                SerializeMultiDimensionalArray(arr, itemType, writer, arrRank);
        }

        void SerializeGeneric(dynamic arr, Type[] arrInterfaces, Type specifiedItemType, ABSaveWriter writer)
        {
            var arrSize = Enumerable.Count(arr); // TODO: Maybe optimize performance for non-collections to require only one iteration?
            writer.WriteInt32((uint)arrSize);

            var perItem = GetSerializeCorrectPerItemOperation(specifiedItemType, writer.Settings, out TypeInformation itemInfo);

            if (ABSaveUtils.HasGenericInterface(arrInterfaces, typeof(IList<>)))
                for (int i = 0; i < arrSize; i++) perItem(arr[i], itemInfo, writer);
            else
                foreach (object item in arr) perItem(item, itemInfo, writer);
        }

        void SerializeNonGeneric(IEnumerable arr, ABSaveWriter writer)
        {
            // TODO: Maybe optimize performance for non-collections to require only one iteration?
            int size = GetNonGenericSize(arr);
            writer.WriteInt32((uint)size);

            var itemInfo = ObjectTypeInfo;

            if (arr is IList list)
                for (int i = 0; i < size; i++) 
                    SerializeItemAuto(list[i], itemInfo, writer);

            else
                foreach (object item in arr) 
                    SerializeItemAuto(item, itemInfo, writer);
        }

        void SerializeItemAuto(object item, TypeInformation typeInfo, ABSaveWriter writer)
        {
            typeInfo.ActualType = item.GetType();
            typeInfo.ActualTypeCode = Type.GetTypeCode(typeInfo.ActualType);
            ABSaveItemSerializer.SerializeAuto(item, typeInfo, writer);
        }

        void SerializeSingleDimensionalArray(Array arr, Type itemType, ABSaveWriter writer)
        {
            int lowerBound = arr.GetLowerBound(0);
            var defaultLowerBound = lowerBound == 0;
            writer.WriteByte(defaultLowerBound ? (byte)0 : (byte)1);

            if (!defaultLowerBound) writer.WriteInt32((uint)lowerBound);
            writer.WriteInt32((uint)arr.Length);

            // Fast write bytes or shorts using the writer's native methods.
            if (TryFastWriteArray(arr, writer, itemType)) return;

            var perItem = GetSerializeCorrectPerItemOperation(itemType, writer.Settings, out TypeInformation itemInfo);
            int endIndex = lowerBound + arr.Length;

            for (int i = lowerBound; i < endIndex; i++) perItem(arr.GetValue(i), itemInfo, writer);
        }

        unsafe void SerializeMultiDimensionalArray(Array arr, Type itemType, ABSaveWriter writer, int arrRank)
        {
            int* lowerBounds = stackalloc int[arrRank];
            int* lengths = stackalloc int[arrRank];

            var defaultLowerBounds = HasDefaultLowerBounds(arr, arrRank, lowerBounds);

            // Write the control byte and number of ranks.
            writer.WriteByte(defaultLowerBounds ? (byte)2 : (byte)3);
            writer.WriteInt32((uint)arrRank);

            // Write the lower bounds.
            if (!defaultLowerBounds)
            {
                for (int i = 0; i < arrRank; i++)
                    writer.WriteInt32((uint)lowerBounds[i]);
            }

            // Write the lengths.
            for (int i = 0; i < arrRank; i++)
            {
                lengths[i] = arr.GetLength(i);
                writer.WriteInt32((uint)lengths[i]);
            }

            SerializeDimension(arr, itemType, writer, new ArrayDimensionInfo(lowerBounds, lengths, arrRank), 0);
        }


        unsafe void SerializeDimension(Array arr, Type itemType, ABSaveWriter writer, in ArrayDimensionInfo dimensionInfo, int dimension)
        {
            int lowerBound = dimensionInfo.LowerBounds[dimension];
            int length = dimensionInfo.Lengths[dimension];
            int[] currentPos = dimensionInfo.CurrentPos;

            int endIndex = lowerBound + length;
            if (dimension == dimensionInfo.EndDimension)
            {
                var perItem = GetSerializeCorrectPerItemOperation(itemType, writer.Settings, out TypeInformation itemInfo);
                for (currentPos[dimension] = lowerBound; currentPos[dimension] < endIndex; currentPos[dimension]++) perItem(arr.GetValue(currentPos), itemInfo, writer);
            }
            else
            {
                int nextDimension = dimension + 1;
                for (currentPos[dimension] = lowerBound; currentPos[dimension] < endIndex; currentPos[dimension]++)
                    SerializeDimension(arr, itemType, writer, dimensionInfo, nextDimension);
            }
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

        unsafe bool TryFastWriteArray(Array arr, ABSaveWriter writer, Type itemType)
        {
            if (itemType == typeof(byte))
                writer.WriteByteArray((byte[])arr, true);

            else if (itemType == typeof(char))
            {
                char[] asCharArr = (char[])arr;
                fixed (char* s = asCharArr)
                    writer.FastWriteShorts((short*)s, arr.Length);
            }
            else if (itemType == typeof(short))
            {
                short[] asShortArr = (short[])arr;
                fixed (short* s = asShortArr)
                    writer.FastWriteShorts(s, arr.Length);
            }
            else if (itemType == typeof(ushort))
            {
                ushort[] asShortArr = (ushort[])arr;
                fixed (ushort* s = asShortArr)
                    writer.FastWriteShorts((short*)s, arr.Length);
            }
            else return false;

            return true;
        }

        #endregion

        #region Helpers

        Action<object, TypeInformation, ABSaveWriter> GetSerializeCorrectPerItemOperation(Type itemType, ABSaveSettings settings, out TypeInformation itemInfo)
        {
            // If the specified type is a value type, then we know for a fact all the items will be the same type, so we only need to find the converter once.
            if (itemType.IsValueType)
            {
                itemInfo = new TypeInformation(itemType, Type.GetTypeCode(itemType));

                if (ABSaveUtils.TryFindConverterForType(settings, itemInfo, out ABSaveTypeConverter converter))
                    return converter.Serialize;
                else
                    return ABSaveItemSerializer.SerializeAuto;
            }

            itemInfo = new TypeInformation(null, TypeCode.Empty, itemType, Type.GetTypeCode(itemType));
            return SerializeItemAuto;
        }

        int GetNonGenericSize(IEnumerable arr)
        {
            if (arr is ICollection collection) return collection.Count;
            else
            {
                int size = 0;
                var enumerator = arr.GetEnumerator();
                while (enumerator.MoveNext()) size++;
                return size;
            }
        }

        bool TryGetIEnumerableGenericArgument(Type[] interfaces, out Type genericArgument)
        {
            for (int i = 0; i < interfaces.Length; i++)
                if (interfaces[i].IsGenericType && interfaces[i].GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    genericArgument = interfaces[i].GetGenericArguments()[0];
                    return true;
                }

            genericArgument = null;
            return false;
        }

        #endregion
    }

    internal unsafe readonly struct ArrayDimensionInfo
    {
        public readonly int* LowerBounds;
        public readonly int* Lengths;
        public readonly int[] CurrentPos;
        public readonly int EndDimension;

        public ArrayDimensionInfo(int* lowerBounds, int* lengths, int arrRank)
        {
            LowerBounds = lowerBounds;
            Lengths = lengths;
            CurrentPos = new int[arrRank];
            EndDimension = arrRank - 1;
        }
    }
}
