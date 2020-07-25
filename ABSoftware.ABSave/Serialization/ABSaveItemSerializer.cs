using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Converters.Internal;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization.Writer;
using System;
using System.ComponentModel;
using System.Reflection;

namespace ABSoftware.ABSave.Serialization
{
    public static class ABSaveItemSerializer
    {
        public static void SerializeAuto(object obj, ABSaveWriter writer, TypeInformation typeInformation)
        {
            SerializeAttributes(obj, writer, typeInformation);

            if (writer.Settings.AutoCheckTypeConverters && AttemptSerializeWithTypeConverter(obj, writer, typeInformation))
                return;

            ABSaveObjectConverter.AutoSerializeObject(obj, writer, typeInformation);
        }

        internal static void SerializeAttributes(object obj, ABSaveWriter writer, TypeInformation typeInformation)
        {
            // If a nullable represents a null item, this will write the null attribute.
            if (obj == null)
            {
                writer.WriteNullAttribute();
                return;
            }

            // NOTE: Because of nullable's unique behaviour with boxing, they must be handled specially here, we must make sure we write an attribute if they aren't null.
            // From here, it will serialize it just like it's a normal data type.
            if (typeInformation.SpecifiedType.IsGenericType && typeInformation.SpecifiedType.GetGenericTypeDefinition() == typeof(Nullable<>))
                writer.WriteMatchingTypeAttribute();
            else if (writer.Settings.WithTypes)
                SerializeTypeBeforeItem(writer, typeInformation.SpecifiedType, typeInformation.ActualType);
        }

        static bool AttemptSerializeWithTypeConverter(object obj, ABSaveWriter writer, TypeInformation typeInformation)
        {
            if (ABSaveUtils.TryFindConverterForType(writer.Settings, typeInformation, out ABSaveTypeConverter typeConverter))
            {
                typeConverter.Serialize(obj, typeInformation, writer);
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
                if (specifiedType != null && actualType != specifiedType)
                {
                    writer.WriteDifferentTypeAttribute();
                    TypeTypeConverter.Instance.SerializeClosedType(actualType, writer);
                }
                else writer.WriteMatchingTypeAttribute();
            }
        }
    }
}