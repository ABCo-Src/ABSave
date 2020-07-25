using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Converters.Internal
{
    public class TypeTypeConverter : ABSaveTypeConverter
    {
        public static TypeTypeConverter Instance = new TypeTypeConverter();
        private TypeTypeConverter() { }

        public override bool HasExactType => true;
        public override Type ExactType => typeof(Type);

        public override void Serialize(object obj, TypeInformation typeInfo, ABSaveWriter writer) => SerializeType((Type)obj, writer);

        public void SerializeType(Type type, ABSaveWriter writer)
        {
            SerializeTypeMainPartAndKey(type, type.IsGenericType ? type.GetGenericTypeDefinition() : type, writer);
            SerializeGenericPart(type, writer);
        }

        public void SerializeClosedType(Type type, ABSaveWriter writer)
        {
            SerializeTypeMainPartAndKey(type, type.IsGenericType ? type.GetGenericTypeDefinition() : type, writer);
            SerializeGenericPart(type, writer);
        }

        public void SerializeTypeMainPartAndKey(Type type, Type genericType, ABSaveWriter writer)
        {
            if (writer.Settings.CacheTypesAndAssemblies && HandleKeyBeforeType(type, writer)) return;

            AssemblyTypeConverter.Instance.Serialize(type.Assembly, new TypeInformation(), writer);
            writer.WriteText(genericType.FullName);
        }

        public void SerializeGenericPart(Type type, ABSaveWriter writer)
        {
            if (type.IsGenericType)
            {
                var generics = type.GetGenericArguments();
                for (int i = 0; i < generics.Length; i++)
                {
                    if (generics[i].IsGenericParameter)
                        writer.WriteByte(1);
                    else
                    {
                        writer.WriteByte(0);
                        SerializeType(generics[i], writer);
                    }
                }
            }
        }

        public void SerializeClosedGenericPart(Type type, ABSaveWriter writer)
        {
            if (type.IsGenericType)
            {
                var generics = type.GetGenericArguments();
                for (int i = 0; i < generics.Length; i++)
                    SerializeClosedType(generics[i], writer);
            }
        }

        internal static bool HandleKeyBeforeType(Type type, ABSaveWriter writer)
        {
            var successful = writer.CachedTypes.TryGetValue(type, out int key);

            if (successful)
            {
                writer.WriteInt32((uint)key);
                return true;
            }
            else if (writer.CachedTypes.Count == int.MaxValue)
                writer.WriteInt32(uint.MaxValue);
            else
            {
                int size = writer.CachedAssemblies.Count;
                writer.CachedTypes.Add(type, size);
                writer.WriteInt32ToSignificantBytes(size, ABSaveUtils.GetRequiredNoOfBytesToStoreNumber(size));
            }

            return false;
        }
    }
}
