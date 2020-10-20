using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Exceptions;
using ABSoftware.ABSave.Mapping;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;

namespace ABSoftware.ABSave
{
    /// <summary>
    /// Serializes items (and their attributes) in an ABSave document.
    /// </summary>
    public static class ABSaveItemConverter
    {
        public static void Serialize(object obj, Type specifiedType, ABSaveWriter writer) => Serialize(obj, obj?.GetType(), specifiedType, writer);

        public static void Serialize(object obj, Type actualType, Type specifiedType, ABSaveWriter writer)
        {
            if (SerializeAttribute(obj, actualType, specifiedType, writer)) return;
            SerializeWithoutAttribute(obj, actualType, writer);
        }

        public static object DeserializeWithAttribute(Type specifiedType, ABSaveReader reader)
        {
            var actualType = DeserializeAttribute(reader, specifiedType);
            if (actualType == null) return null;

            return DeserializeWithoutAttribute(actualType, reader);
        }

        public static void SerializeWithoutAttribute(object obj, Type actualType, ABSaveWriter writer)
        {
            if (ABSaveUtils.TryFindConverterForType(writer.Settings, actualType, out ABSaveTypeConverter typeConverter))
                typeConverter.Serialize(obj, actualType, writer);

            else ABSaveObjectConverter.Serialize(obj, actualType, writer);
        }

        public static object DeserializeWithoutAttribute(Type actualType, ABSaveReader reader)
        {
            if (ABSaveUtils.TryFindConverterForType(reader.Settings, actualType, out ABSaveTypeConverter typeConverter))
                return typeConverter.Deserialize(actualType, reader);

            return ABSaveObjectConverter.Deserialize(actualType, reader);
        }

        public static bool SerializeAttribute(object obj, Type actualType, Type specifiedType, ABSaveWriter writer)
        {
            if (obj == null)
            {
                writer.WriteNullAttribute();
                return true;
            }

            if (specifiedType.IsValueType)
            {
                // NOTE: Because of nullable's unique behaviour with boxing (resulting in a different "actual type"),
                //       they must be handled specially here, to make sure we write an attribute if their value isn't null.
                if (IsNullable(specifiedType))
                    writer.WriteMatchingTypeAttribute();
            }
            else if (specifiedType == actualType) writer.WriteMatchingTypeAttribute();
            else
            {
                writer.WriteDifferentTypeAttribute();
                TypeTypeConverter.Instance.SerializeClosedType(actualType, writer);
            }

            return false;
        }

        /// <summary>
        /// Returns the actual type of data specified by the attribute, or null if there is none.
        /// </summary>
        public static Type DeserializeAttribute(ABSaveReader reader, Type specifiedType)
        {
            if (specifiedType.IsValueType)
            {
                if (IsNullable(specifiedType))
                    return (reader.ReadByte() == 1) ? null : specifiedType.GetGenericArguments()[0];

                return specifiedType;
            }

            return reader.ReadByte() switch
            {
                1 => null,
                2 => specifiedType,
                3 => TypeTypeConverter.Instance.DeserializeClosedType(reader),
                _ => throw new ABSaveInvalidDocumentException(reader.Source.Position),
            };
        }

        static bool IsNullable(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }
}