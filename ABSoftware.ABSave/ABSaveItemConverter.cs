using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Serialization;
using System;
using System.ComponentModel;
using System.Reflection;

namespace ABSoftware.ABSave
{
    public static class ABSaveItemConverter
    {
        public static void Serialize(object obj, Type specifiedType, ABSaveWriter writer) => Serialize(obj, obj.GetType(), specifiedType, writer);

        public static void Serialize(object obj, Type actualType, Type specifiedType, ABSaveWriter writer)
        {
            if (SerializeAttributes(obj, actualType, specifiedType, writer)) return;

            if (writer.Settings.AutoCheckTypeConverters && AttemptSerializeWithTypeConverter(obj, actualType, writer))
                return;

            ABSaveObjectConverter.Serialize(obj, actualType, writer);
        }

        public static object Deserialize(Type specifiedType, ABSaveReader reader)
        {
            // TODO
        }

        public static void Serialize(object obj, Type specifiedType, ABSaveWriter writer, ABSaveMapItem mapItem)
        {
            if (mapItem == null)
            {
                Serialize(obj, specifiedType, writer);
                return;
            }

            var actualType = obj.GetType();

            if (SerializeAttributes(obj, actualType, specifiedType, writer)) return;
            mapItem.Serialize(obj, actualType, writer);
        }

        internal static bool SerializeAttributes(object obj, Type actualType, Type specifiedType, ABSaveWriter writer)
        {
            // If a nullable represents a null item, this will write the null attribute.
            if (obj == null)
            {
                writer.WriteNullAttribute();
                return true;
            }

            // NOTE: Because of nullable's unique behaviour with boxing, they must be handled specially here, we must make sure we write an attribute if they aren't null.
            // From here, it will serialize it just like it's a normal data type.
            if (typeInformation.SpecifiedType.IsGenericType && typeInformation.SpecifiedType.GetGenericTypeDefinition() == typeof(Nullable<>))
                writer.WriteMatchingTypeAttribute();
            else
                SerializeTypeBeforeItem(writer, typeInformation.SpecifiedType, typeInformation.ActualType);

            return false;
        }

        static bool AttemptSerializeWithTypeConverter(object obj, Type type, ABSaveWriter writer)
        {
            if (ABSaveUtils.TryFindConverterForType(writer.Settings, type, out ABSaveTypeConverter typeConverter))
            {
                typeConverter.Serialize(obj, type, writer);
                return true;
            }

            return false;
        }

        public static void SerializeTypeBeforeItem(ABSaveWriter writer, Type specifiedType, Type actualType)
        {
            // If the specified type is a value type, then there's no need to write any type information about it, since we know for certain nothing can inherit from it, and by extension no other type of data can be put there.
            if (!specifiedType.IsValueType)
            {
                // Remember that if the main part of the type is the same, the generics cannot be different, it's only if the main part is different do we need to write generics as well.
                if (actualType != specifiedType)
                {
                    writer.WriteDifferentTypeAttribute();
                    TypeTypeConverter.Instance.SerializeClosedType(actualType, writer);
                }
                else writer.WriteMatchingTypeAttribute();
            }
        }
    }
}