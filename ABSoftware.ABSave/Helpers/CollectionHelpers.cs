using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Mapping;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Helpers
{
    internal static class CollectionHelpers
    {
        public static Action<object, Type, ABSaveWriter, ABSaveTypeConverter> GetSerializeCorrectPerItemOperation(Type itemType, ABSaveSettings settings, out ABSaveTypeConverter converter)
        {
            converter = null;

            if (itemType.IsValueType)
                if (ABSaveUtils.TryFindConverterForType(settings, itemType, out converter))
                    return (item, specifiedType, writer, c) => c.Serialize(item, specifiedType, writer);
                else
                    return (item, specifiedType, writer, c) => ABSaveItemConverter.Serialize(item, specifiedType, specifiedType, writer);

            return (item, specifiedType, writer, c) => ABSaveItemConverter.Serialize(item, specifiedType, writer);
        }

        public static Func<Type, ABSaveReader, ABSaveTypeConverter, object> GetDeserializeCorrectPerItemOperation(Type itemType, ABSaveSettings settings, out ABSaveTypeConverter converter)
        {
            converter = null;

            // If the specified type is a value type and there's a converter for it, then we know all the items will be the same type, so we only need to find the converter once.
            if (itemType.IsValueType && ABSaveUtils.TryFindConverterForType(settings, itemType, out converter))
                return (specifiedType, reader, c) => c.Deserialize(specifiedType, reader);

            return (specifiedType, reader, c) => ABSaveItemConverter.DeserializeWithAttribute(specifiedType, reader);
        }
    }
}
