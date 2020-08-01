using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping;
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

        static TypeInformation ObjectTypeInfo = new TypeInformation(null, TypeCode.Empty, typeof(object), TypeCode.Object);

        public override bool HasExactType => false;
        public override bool CheckCanConvertType(TypeInformation typeInfo) => typeInfo.ActualType.IsArray || ABSaveUtils.HasInterface(typeInfo.ActualType, typeof(IEnumerable));

        #region Serialization

        public override void Serialize(object obj, TypeInformation typeInfo, ABSaveWriter writer)
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

        void SerializeArray(Array arr, ABSaveWriter writer, Type itemType, ABSaveMapItem perItemMap)
        {
            var arrRank = arr.Rank;

            if (arrRank == 1)
                SerializeSingleDimensionalArray(arr, itemType, writer, perItemMap);
            else
                SerializeMultiDimensionalArray(arr, itemType, writer, perItemMap, arrRank);
        }

        private void SerializeByType(object obj, ABSaveWriter writer, Type specifiedItem, CollectionType collectionType, ABSaveMapItem perItemMap)
        {
            switch (collectionType)
            {
                case CollectionType.Generic:
                case CollectionType.GenericIList:
                    var perItem = GetSerializeCorrectPerItemOperation(specifiedItem, writer.Settings, perItemMap, out TypeInformation itemInfo);
                    var asDynamic = (dynamic)obj;

                    var arrSize = asDynamic.Count;
                    writer.WriteInt32((uint)arrSize);

                    if (collectionType == CollectionType.GenericIList)
                        for (int i = 0; i < arrSize; i++) perItem(asDynamic[i], itemInfo, writer, perItemMap);
                    else
                        foreach (object item in asDynamic) perItem(item, itemInfo, writer, perItemMap);

                    break;

                case CollectionType.NonGeneric:
                    var asCollection = (ICollection)obj;
                    writer.WriteInt32((uint)asCollection.Count);

                    foreach (object item in asCollection)
                        AutoSerializeItem(item, ObjectTypeInfo, writer, perItemMap);

                    break;

                case CollectionType.NonGenericIList:
                    var asList = (IList)obj;
                    var size = asList.Count;
                    writer.WriteInt32((uint)size);

                    for (int i = 0; i < size; i++)
                        AutoSerializeItem(asList[i], ObjectTypeInfo, writer, perItemMap);

                    break;
            }
        }

        #region Array

        void SerializeSingleDimensionalArray(Array arr, Type itemType, ABSaveWriter writer, ABSaveMapItem perItemMap)
        {
            int lowerBound = arr.GetLowerBound(0);
            var defaultLowerBound = lowerBound == 0;
            writer.WriteByte(defaultLowerBound ? (byte)0 : (byte)1);

            if (!defaultLowerBound) writer.WriteInt32((uint)lowerBound);
            writer.WriteInt32((uint)arr.Length);

            // Fast write bytes or shorts using the writer's native methods.
            if (TryFastWriteArray(arr, writer, itemType)) return;

            var perItem = GetSerializeCorrectPerItemOperation(itemType, writer.Settings, perItemMap, out TypeInformation itemInfo);
            int endIndex = lowerBound + arr.Length;

            for (int i = lowerBound; i < endIndex; i++) perItem(arr.GetValue(i), itemInfo, writer, perItemMap);
        }

        unsafe void SerializeMultiDimensionalArray(Array arr, Type itemType, ABSaveWriter writer, ABSaveMapItem perItemMap, int arrRank)
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

            SerializeDimension(arr, itemType, writer, perItemMap, new ArrayDimensionInfo(lowerBounds, lengths, arrRank), 0);
        }

        unsafe void SerializeDimension(Array arr, Type itemType, ABSaveWriter writer, ABSaveMapItem perItemMap, in ArrayDimensionInfo dimensionInfo, int dimension)
        {
            int lowerBound = dimensionInfo.LowerBounds[dimension];
            int length = dimensionInfo.Lengths[dimension];
            int[] currentPos = dimensionInfo.CurrentPos;

            int endIndex = lowerBound + length;
            if (dimension == dimensionInfo.EndDimension)
            {
                var perItem = GetSerializeCorrectPerItemOperation(itemType, writer.Settings, perItemMap, out TypeInformation itemInfo);
                for (currentPos[dimension] = lowerBound; currentPos[dimension] < endIndex; currentPos[dimension]++) perItem(arr.GetValue(currentPos), itemInfo, writer, perItemMap);
            }
            else
            {
                int nextDimension = dimension + 1;
                for (currentPos[dimension] = lowerBound; currentPos[dimension] < endIndex; currentPos[dimension]++)
                    SerializeDimension(arr, itemType, writer, perItemMap, dimensionInfo, nextDimension);
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

        #endregion

        void AutoSerializeItem(object item, TypeInformation typeInfo, ABSaveWriter writer, ABSaveMapItem map)
        {
            typeInfo.ActualType = item.GetType();
            typeInfo.ActualTypeCode = Type.GetTypeCode(typeInfo.ActualType);
            ABSaveItemSerializer.Serialize(item, typeInfo, writer);
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

        Action<object, TypeInformation, ABSaveWriter, ABSaveMapItem> GetSerializeCorrectPerItemOperation(Type itemType, ABSaveSettings settings, ABSaveMapItem perItemMap, out TypeInformation itemInfo)
        {
            if (perItemMap != null) 
            {
                itemInfo = new TypeInformation(itemType, Type.GetTypeCode(itemType));

                return (item, typeInfo, writer, map) =>
                {
                    typeInfo.ActualType = item.GetType();
                    typeInfo.ActualTypeCode = Type.GetTypeCode(typeInfo.ActualType);
                    perItemMap.Serialize(item, typeInfo, writer);
                };
            }

            // If the specified type is a value type, then we know for a fact all the items will be the same type, so we only need to find the converter once.
            if (itemType.IsValueType)
            {
                itemInfo = new TypeInformation(itemType, Type.GetTypeCode(itemType));

                if (ABSaveUtils.TryFindConverterForType(settings, itemInfo, out ABSaveTypeConverter converter))
                    return (item, typeInfo, writer, map) => converter.Serialize(item, typeInfo, writer);
                else
                    return (item, typeInfo, writer, map) => ABSaveItemSerializer.Serialize(item, typeInfo, writer);
            }

            itemInfo = new TypeInformation(null, TypeCode.Empty, itemType, Type.GetTypeCode(itemType));
            return AutoSerializeItem;
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
                        genericICollection = interfaces[i];

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
