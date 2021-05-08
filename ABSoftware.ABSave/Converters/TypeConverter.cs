using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Mapping.Generation;
using ABSoftware.ABSave.Serialization;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace ABSoftware.ABSave.Converters
{
    public class TypeConverter : Converter
    {
        public static TypeConverter Instance { get; } = new TypeConverter();
        private TypeConverter() { }

        public override bool ConvertsSubTypes => true;
        public override bool AlsoConvertsNonExact => true;
        public override bool WritesToHeader => true;
        public override Type[] ExactTypes { get; } = new Type[] { typeof(Type) };

        #region Serialize

        public override void Serialize(object obj, Type actualType, IConverterContext context, ref BitTarget header) => SerializeType((Type)obj, ref header);

        public void SerializeType(Type type, ref BitTarget header) => SerializeType(type, ref header, SerializeGenerics);
        public void SerializeClosedType(Type type, ref BitTarget header) => SerializeType(type, ref header, SerializeClosedGenerics);

        static void SerializeType(Type type, ref BitTarget header, Action<Type, ABSaveSerializer> genericHandler)
        {
            // Try to use a pre-saved one
            if (HandleSerializeSaved(type, ref header)) return;

            // Convert the type
            if (type.IsGenericType)
            {
                SerializeTypeMainPart(type.GetGenericTypeDefinition(), ref header);
                genericHandler(type, header.Serializer);
            }
            else SerializeTypeMainPart(type, ref header);
        }

        static void SerializeTypeMainPart(Type type, ref BitTarget header)
        {
            header.Serializer.SerializeExactNonNullItem(type.Assembly, header.Serializer.Map.AssemblyItem, ref header);
            header.Serializer.WriteString(type.FullName);
        }

        #endregion

        #region Serialize Generics

        void SerializeGenerics(Type type, ABSaveSerializer serializer)
        {
            var generics = type.GetGenericArguments();
            var currentTarget = new BitTarget(serializer);

            for (int i = 0; i < generics.Length; i++)
            {
                if (generics[i].IsGenericParameter)
                    currentTarget.WriteBitOff();
                else
                {
                    currentTarget.WriteBitOn();
                    SerializeType(generics[i], ref currentTarget);
                }
            }
        }

        void SerializeClosedGenerics(Type type, ABSaveSerializer serializer)
        {
            var generics = type.GetGenericArguments();
            var target = new BitTarget(serializer);

            for (int i = 0; i < generics.Length; i++) SerializeClosedType(generics[i], ref target);
        }

        #endregion

        #region Deserialize

        public override object Deserialize(Type actualType, IConverterContext context, ref BitSource header) => DeserializeType(ref header);

        public Type DeserializeType(ref BitSource header) => DeserializeType(ref header, DeserializeOpenGenerics);
        public Type DeserializeClosedType(ref BitSource header) => DeserializeType(ref header, DeserializeClosedGenerics);

        static Type DeserializeType(ref BitSource header, Func<Type, ABSaveDeserializer, Type> genericAction)
        {
            // Try use a saved value.
            var cachedType = HandleDeserializeSaved(ref header);
            if (cachedType != null) return cachedType;

            // Convert the type
            var mainPart = DeserializeTypeMainPart(ref header);
            var withGenerics = mainPart.IsGenericType ? genericAction(mainPart, header.Deserializer) : mainPart;

            // Save the type
            header.Deserializer.SavedTypes.Add(withGenerics);
            return withGenerics;
        }

        public static Type DeserializeTypeMainPart(ref BitSource header)
        {
            var assembly = (Assembly)header.Deserializer.DeserializeExactNonNullItem(header.Deserializer.Map.AssemblyItem, ref header)!;
            var typeName = header.Deserializer.ReadString();

            return assembly.GetType(typeName)!;
        }

        #endregion

        #region Deserialize Generics

        public Type DeserializeOpenGenerics(Type mainPart, ABSaveDeserializer deserializer)
        {
            var parameters = mainPart.GetGenericArguments();
            var currentSource = new BitSource(deserializer);

            for (int i = 0; i < parameters.Length; i++)
                if (currentSource.ReadBit())
                    parameters[i] = DeserializeType(ref currentSource);
            
            return mainPart.MakeGenericType(parameters);
        }

        public Type DeserializeClosedGenerics(Type mainPart, ABSaveDeserializer deserializer)
        {
            var parameters = mainPart.GetGenericArguments();
            var header = new BitSource(deserializer);

            for (int i = 0; i < parameters.Length; i++) parameters[i] = DeserializeClosedType(ref header);
        
            return mainPart.MakeGenericType(parameters);
        }

        //public Type DeserializeDifferenceGenerics(Type original, MapItem assemblyMap, ref BitSource source)
        //{
        //    var parameters = original.GetGenericArguments();
        //    var currentSource = new BitSource(source.Deserializer);

        //    for (int i = 0; i < parameters.Length; i++)
        //        if (currentSource.ReadBit())
        //            parameters[i] = DeserializeType(assemblyMap, ref currentSource);

        //    return original.MakeGenericType(parameters);
        //}

        #endregion

        static bool HandleSerializeSaved(Type type, ref BitTarget header)
        {
            var isSaved = header.Serializer.SavedTypes.TryGetValue(type, out int key);

            if (isSaved)
            {
                header.WriteBitOn();
                header.Serializer.WriteCompressed((uint)key, ref header);
                return true;
            }

            header.WriteBitOff();
            header.Serializer.SavedTypes.Add(type, header.Serializer.SavedTypes.Count);
            return false;
        }

        static Type? HandleDeserializeSaved(ref BitSource header)
        {
            var isSaved = header.ReadBit();

            if (isSaved)
            {
                var key = (int)header.Deserializer.ReadCompressedInt(ref header);
                return header.Deserializer.SavedTypes[key];
            }

            return null;
        }

        public override IConverterContext? TryGenerateContext(ref ContextGen gen)
        {
            if (gen.Type == typeof(Type) || gen.Type.IsSubclassOf(typeof(Type))) gen.MarkCanConvert();
            
            return null;
        }
    }
}
