using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Mapping;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Helpers
{
    internal static class CollectionHelpers
    {
        public static Action<object, Type, ABSaveWriter, ABSaveMapItem> GetSerializeCorrectPerItemOperation(Type itemType, ABSaveSettings settings, bool? mapElementsSameType)
        {
            if (mapElementsSameType.HasValue)
                if (mapElementsSameType == true)
                    return (item, specifiedType, writer, map) => map.Serialize(item, specifiedType, writer);
                else
                    return (item, specifiedType, writer, map) => map.Serialize(item, item.GetType(), writer);

            // If the specified type is a value type, then we know all the items will be the same type, so we only need to work out what to do once.
            if (itemType.IsValueType)
                if (ABSaveUtils.TryFindConverterForType(settings, itemType, out ABSaveTypeConverter converter))
                    return (item, specifiedType, writer, map) => converter.Serialize(item, specifiedType, writer);
                else
                    return (item, specifiedType, writer, map) => ABSaveItemConverter.SerializeWithAttribute(item, itemType, specifiedType, writer);

            return (item, specifiedType, writer, map) => ABSaveItemConverter.SerializeWithAttribute(item, specifiedType, writer);
        }

        public static Func<Type, ABSaveReader, ABSaveMapItem, object> GetDeserializeCorrectPerItemOperation(Type itemType, ABSaveSettings settings, ABSaveMapItem perItemMap)
        {
            if (perItemMap != null)
                return (specifiedType, reader, map) => map.Deserialize(specifiedType, reader);

            // If the specified type is a value type and there's a converter for it, then we know all the items will be the same type, so we only need to find the converter once.
            if (itemType.IsValueType && ABSaveUtils.TryFindConverterForType(settings, itemType, out ABSaveTypeConverter converter))
                return (specifiedType, reader, map) => converter.Deserialize(specifiedType, reader);

            return (specifiedType, reader, map) => ABSaveItemConverter.DeserializeWithAttribute(specifiedType, reader);
        }
    }
}
