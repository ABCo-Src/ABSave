using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave.Serialization
{
    public static class ABSaveSerializer
    {
        /// <returns>Used internally by ABSave, describes whether ABSave needs to put a NextItem character after this object when serialized.</returns>
        public static void SerializeAuto(object obj, ABSaveWriter writer)
        {
            var type = obj.GetType();
            SerializeAuto(obj, writer, new TypeInformation(type, Type.GetTypeCode(type)));
        }

        /// <returns>Used internally by ABSave, describes whether ABSave needs to put a NextItem character after this object when serialized.</returns>
        public static void SerializeAuto(object obj, ABSaveWriter writer, TypeInformation typeInformation)
        {
            if (writer.Settings.AutoCheckTypeConverters && AttemptSerializeWithTypeConverter(obj, writer, typeInformation))
                return;
            if (writer.Settings.AutoCheckStringConverters && AttemptSerializeWithStringConverter(obj, writer, typeInformation))
                return;

            ABSaveObjectConverter.AutoSerializeObject(obj, writer, typeInformation);
        }

        public static bool AttemptSerializeWithTypeConverter(object obj, ABSaveWriter writer, TypeInformation typeInformation)
        {
            var absaveConverter = ABSaveTypeConverter.FindTypeConverterForType(typeInformation);
            if (absaveConverter != null)
            {
                if (writer.Settings.WithTypes)
                    SerializeTypeBeforeItem(writer, typeInformation.SpecifiedType, typeInformation.ActualType);

                absaveConverter.Serialize(obj, writer, typeInformation);
                return true;
            }

            return false;
        }

        public static bool AttemptSerializeWithStringConverter(object obj, ABSaveWriter writer, TypeInformation typeInformation)
        {
            var typeConv = TypeDescriptor.GetConverter(typeInformation.ActualType);
            if (typeConv.IsValid(obj))
                if (typeConv.CanConvertTo(typeof(string)) || typeConv.CanConvertFrom(typeof(string)))
                {
                    if (writer.Settings.WithTypes)
                        SerializeTypeBeforeItem(writer, typeInformation.SpecifiedType, typeInformation.ActualType);

                    writer.WriteText(typeConv.ConvertToString(obj));
                    return true;
                }

            return false;
        }

        public static void SerializeString(string str, ABSaveWriter writer) => writer.WriteText(str);

        public static void SerializeType(Type type, ABSaveWriter writer)
        {
            SerializeType(type, type.GetGenericTypeDefinition(), writer);
        }

        public static void SerializeType(Type type, Type genericType, ABSaveWriter writer, bool storeIfGeneric = true)
        {
            if (HandleKeyBeforeType(type, writer)) return;
            SerializeAssembly(type.Assembly, writer);
            writer.WriteText(genericType.FullName);

            if (storeIfGeneric)
            {
                if (type.IsGenericTypeDefinition)
                {
                    writer.WriteByte(1);
                    return;
                } else writer.WriteByte(0);
            }
            
            var generics = type.GenericTypeArguments;
            for (int i = 0; i < generics.Length; i++)
                SerializeType(type, type.GetGenericTypeDefinition(), writer, storeIfGeneric);
        }

        internal static bool HandleKeyBeforeType(Type type, ABSaveWriter writer)
        {
            var successful = ABSaveUtils.SearchForCachedType(writer, type, out byte[] key);

            if (successful)
            {
                writer.WriteByteArray(key, false);
                return true;
            }

            // If we can't save anymore types, just write 65,535.
            else if (writer.CachedTypes.Count == ushort.MaxValue)
            {
                writer.WriteByte(255);
                writer.WriteByte(255);
            }
            else
                writer.WriteByteArray(ABSaveUtils.CreateCachedType(type, writer), false);

            return false;
        }

        public static void SerializeAssembly(Assembly assembly, ABSaveWriter writer)
        {
            var successful = ABSaveUtils.SearchForCachedAssembly(writer, assembly, out byte key);

            if (successful)
            {
                writer.WriteByte(key);
                return;
            }
            else if (writer.CachedAssemblies.Count == byte.MaxValue)
                writer.WriteByte(255);
            else
                writer.WriteByte(ABSaveUtils.CreateCachedAssembly(assembly, writer));

            var assemblyName = assembly.GetName();
            var publicKeyToken = assemblyName.GetPublicKeyToken();
            var hasCulture = assemblyName.CultureName != "";
            var hasPublicKeyToken = publicKeyToken != null;

            // First Byte: 000000(1 for culture)(1 for PublicKeyToken)
            var firstByte = (hasCulture ? 2 : 0) & (hasPublicKeyToken ? 1 : 0);
            writer.WriteByte((byte)firstByte);
            writer.WriteText(assemblyName.Name);
            SerializeVersion(assemblyName.Version, writer);

            if (hasCulture)
                writer.WriteText(assemblyName.CultureName);

            if (hasPublicKeyToken)
                writer.WriteByteArray(publicKeyToken, false);
        }

        public static void SerializeVersion(Version version, ABSaveWriter writer)
        {
            var numberOfDecimals = ABSaveUtils.VersionNumberOfDecimals(version);

            writer.WriteByte(numberOfDecimals);

            if (numberOfDecimals >= 1) writer.WriteInt32((uint)version.Major);
            if (numberOfDecimals >= 2) writer.WriteInt32((uint)version.Major);
            if (numberOfDecimals >= 3) writer.WriteInt32((uint)version.Build);
            if (numberOfDecimals == 4) writer.WriteInt32((uint)version.Revision);
        }

        public static void SerializeTypeBeforeItem(ABSaveWriter writer, Type specifiedType, Type actualType)
        {
            // Write the type itself.
            Type actualTypeGeneric = actualType.GetGenericTypeDefinition();

            if (specifiedType != null && actualTypeGeneric != specifiedType.GetGenericTypeDefinition())
            {
                writer.WriteDifferentTypeAttribute();
                SerializeType(actualType, actualTypeGeneric, writer, false);
            }
            else writer.WriteMatchingTypeAttribute();
        }
    }
}