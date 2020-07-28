using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization;
using ABSoftware.ABSave.Serialization.Writer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace ABSoftware.ABSave.Converters.Internal
{
    public class CollectionTypeConverter : ABSaveTypeConverter
    {
        public readonly static CollectionTypeConverter Instance = new CollectionTypeConverter();
        private CollectionTypeConverter() { }

        static TypeInformation ObjectTypeInfo = new TypeInformation(null, TypeCode.Empty, typeof(object), TypeCode.Object);

        public override bool HasExactType => false;
        public override bool CheckCanConvertType(TypeInformation typeInfo) => typeInfo.ActualType.IsArray || ABSaveUtils.HasInterface(typeInfo.ActualType, typeof(IEnumerable));

        public override void Serialize(object obj, TypeInformation typeInfo, ABSaveWriter writer)
        {
            if (typeInfo.ActualType.IsArray) SerializeArray((Array)obj, typeInfo, writer);
            else 
            {
                var interfaces = typeInfo.ActualType.GetInterfaces();
                if (TryGetIEnumerableGenericArgument(interfaces, out Type specifiedItem)) SerializeGeneric((dynamic)obj, interfaces, specifiedItem, writer);
                else SerializeNonGeneric((IEnumerable)obj, writer);
            }
        }

        private void SerializeArray(Array arr, TypeInformation typeInfo, ABSaveWriter writer)
        {
            writer.WriteInt32((uint)arr.Length);

            var itemType = typeInfo.ActualType.GetElementType();
            var itemInfo = new TypeInformation(null, TypeCode.Empty, itemType, Type.GetTypeCode(itemType));
            for (int i = 0; i < arr.Length; i++) SerializeItem(arr.GetValue(i), writer, itemInfo);
        }

        void SerializeGeneric(dynamic arr, Type[] arrInterfaces, Type specifiedItemType, ABSaveWriter writer)
        {
            var arrSize = Enumerable.Count(arr); // TODO: Maybe optimize performance for non-collections to require only one iteration?
            writer.WriteInt32((uint)arrSize);

            var itemInfo = new TypeInformation(null, TypeCode.Empty, specifiedItemType, Type.GetTypeCode(specifiedItemType));

            if (ABSaveUtils.HasGenericInterface(arrInterfaces, typeof(IList<>)))
                for (int i = 0; i < arrSize; i++) 
                    SerializeItem(arr[i], writer, itemInfo);

            else
                foreach (object item in arr) 
                    SerializeItem(item, writer, itemInfo);
        }

        void SerializeNonGeneric(IEnumerable arr, ABSaveWriter writer)
        {
            // TODO: Maybe optimize performance for non-collections to require only one iteration?
            int size = GetNonGenericSize(arr);
            writer.WriteInt32((uint)size);

            var itemInfo = ObjectTypeInfo;

            if (arr is IList list)
                for (int i = 0; i < size; i++) 
                    SerializeItem(list[i], writer, itemInfo);

            else
                foreach (object item in arr) 
                    SerializeItem(item, writer, itemInfo);
        }

        void SerializeItem(object item, ABSaveWriter writer, TypeInformation typeInfo)
        {
            typeInfo.ActualType = item.GetType();
            typeInfo.ActualTypeCode = Type.GetTypeCode(typeInfo.ActualType);
            ABSaveItemSerializer.SerializeAuto(item, writer, typeInfo);
        }

        #region Helpers

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
}
