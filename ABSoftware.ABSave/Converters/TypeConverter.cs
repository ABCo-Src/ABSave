using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Serialization;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace ABSoftware.ABSave.Converters
{
    public class TypeConverter : ABSaveConverter
    {
        public static TypeConverter Instance = new TypeConverter();
        private TypeConverter() { }

        public override bool ConvertsSubTypes => true;
        public override bool AlsoConvertsNonExact => true;
        public override bool WritesToHeader => true;
        public override Type[] ExactTypes { get; } = new Type[] { typeof(Type) };

        #region Serialize

        public override void Serialize(object obj, Type actualType, IABSaveConverterContext context, ref BitTarget header) => SerializeType((Type)obj, ((Context)context).AssemblyMap, ref header);

        public void SerializeType(Type type, MapItem assemblyMap, ref BitTarget header) => SerializeType(type, assemblyMap, ref header, SerializeGenerics);
        public void SerializeClosedType(Type type, MapItem assemblyMap, ref BitTarget header) => SerializeType(type, assemblyMap, ref header, SerializeClosedGenerics);

        void SerializeType(Type type, MapItem assemblyMap, ref BitTarget header, Action<Type, MapItem, ABSaveSerializer> genericHandler)
        {
            // Try to use a pre-saved one
            if (HandleSerializeSaved(type, ref header)) return;

            // Convert the type
            if (type.IsGenericType)
            {
                SerializeTypeMainPart(type.GetGenericTypeDefinition(), assemblyMap, ref header);
                genericHandler(type, assemblyMap, header.Serializer);
            }
            else SerializeTypeMainPart(type, assemblyMap, ref header);
        }

        //public void SerializeClosedDifferences(Type specifiedType, Type actualType, MapItem assemblyMap, ref BitTarget header)
        //{
        //    // Try to use a pre-saved one
        //    if (HandleSerializeSaved(actualType, ref header)) return;

        //    // Convert the type
        //    if (actualType.IsGenericType)
        //    {
        //        var actualTypeGTD = actualType.GetGenericTypeDefinition();

        //        // Only generic difference
        //        if (specifiedType.GetGenericTypeDefinition() == actualTypeGTD)
        //        {
        //            header.WriteBitOn();
        //            SerializeClosedDifferencesGeneric(specifiedType, actualType, assemblyMap, ref header);
        //        }

        //        // Complete difference
        //        else
        //        {
        //            header.WriteBitOff();

        //            SerializeTypeMainPart(actualType.GetGenericTypeDefinition(), assemblyMap, ref header);
        //            SerializeClosedGenerics(actualType, assemblyMap, header.Serializer);
        //        }
        //    }

        //    else SerializeTypeMainPart(actualType, assemblyMap, ref header);
        //}

        void SerializeTypeMainPart(Type type, MapItem assemblyMap, ref BitTarget header)
        {
            header.Serializer.SerializeExactNonNullItem(type.Assembly, assemblyMap, ref header);
            header.Serializer.WriteString(type.FullName);
        }

        #endregion

        #region Serialize Generics

        void SerializeGenerics(Type type, MapItem assemblyMap, ABSaveSerializer serializer)
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
                    SerializeType(generics[i], assemblyMap, ref currentTarget);
                }
            }
        }

        void SerializeClosedGenerics(Type type, MapItem assemblyMap, ABSaveSerializer serializer)
        {
            var generics = type.GetGenericArguments();
            var target = new BitTarget(serializer);

            for (int i = 0; i < generics.Length; i++) SerializeClosedType(generics[i], assemblyMap, ref target);
        }

        //void SerializeClosedDifferencesGeneric(Type specifiedType, Type actualType, MapItem assemblyMap, ref BitTarget header)
        //{
        //    var specifiedTypeGenerics = specifiedType.GetGenericArguments();
        //    var actualTypeGenerics = actualType.GetGenericArguments();

        //    for (int i = 0; i < specifiedTypeGenerics.Length; i++)
        //    {
        //        if (specifiedTypeGenerics[i] == actualTypeGenerics[i])
        //            header.WriteBitOff();
        //        else
        //        {
        //            header.WriteBitOn();
        //            SerializeType(actualTypeGenerics[i], assemblyMap, ref header);
        //        }
        //    }
        //}

        #endregion

        #region Deserialize

        public override object Deserialize(Type actualType, IABSaveConverterContext context, ref BitSource header) => DeserializeType(((Context)context).AssemblyMap, ref header);

        public Type DeserializeType(MapItem assemblyMap, ref BitSource header) => DeserializeType(ref header, assemblyMap, DeserializeOpenGenerics);
        public Type DeserializeClosedType(MapItem assemblyMap, ref BitSource header) => DeserializeType(ref header, assemblyMap, DeserializeClosedGenerics);

        Type DeserializeType(ref BitSource header, MapItem assemblyMap, Func<Type, MapItem, ABSaveDeserializer, Type> genericAction)
        {
            // Try use a saved value.
            var cachedType = HandleDeserializeSaved(ref header);
            if (cachedType != null) return cachedType;

            // Convert the type
            var mainPart = DeserializeTypeMainPart(assemblyMap, ref header);
            var withGenerics = mainPart.IsGenericType ? genericAction(mainPart, assemblyMap, header.Deserializer) : mainPart;

            // Save the type
            header.Deserializer.SavedTypes.Add(withGenerics);
            return withGenerics;
        }

        //public Type DeserializeClosedDifferences(Type specifiedType, MapItem assemblyMap, ref BitSource header)
        //{
        //    // Try use a saved value.
        //    var cachedType = HandleDeserializeSaved(ref header);
        //    if (cachedType != null) return cachedType;

        //    // Convert the type
        //    var res = ConvertType(ref header);

        //    Type ConvertType(ref BitSource header)
        //    {
        //        if (specifiedType.IsGenericType)
        //        {
        //            var onlyGenericDifference = header.ReadBit();

        //            // Different Generics
        //            if (onlyGenericDifference) return DeserializeDifferenceGenerics(specifiedType, assemblyMap, ref header);

        //            // Completely Different Type
        //            else
        //            {
        //                var mainType = DeserializeTypeMainPart(assemblyMap, ref header);
        //                return DeserializeClosedGenerics(mainType, assemblyMap, header.Deserializer);
        //            }
        //        }

        //        return DeserializeTypeMainPart(assemblyMap, ref header);
        //    }

        //    // Save the type
        //    header.Deserializer.SavedTypes.Add(res);
        //    return res;
        //}

        public Type DeserializeTypeMainPart(MapItem assemblyMap, ref BitSource header)
        {
            var assembly = (Assembly)header.Deserializer.DeserializeExactNonNullItem(assemblyMap, ref header);
            var typeName = header.Deserializer.ReadString();

            return assembly.GetType(typeName);
        }

        #endregion

        #region Deserialize Generics

        public Type DeserializeOpenGenerics(Type mainPart, MapItem assemblyMap, ABSaveDeserializer deserializer)
        {
            var parameters = mainPart.GetGenericArguments();
            var currentSource = new BitSource(deserializer);

            for (int i = 0; i < parameters.Length; i++)
                if (currentSource.ReadBit())
                    parameters[i] = DeserializeType(assemblyMap, ref currentSource);                
            
            return mainPart.MakeGenericType(parameters);
        }

        public Type DeserializeClosedGenerics(Type mainPart, MapItem assemblyMap, ABSaveDeserializer deserializer)
        {
            var parameters = mainPart.GetGenericArguments();
            var header = new BitSource(deserializer);

            for (int i = 0; i < parameters.Length; i++) parameters[i] = DeserializeClosedType(assemblyMap, ref header);            
        
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

        Type HandleDeserializeSaved(ref BitSource header)
        {
            var isSaved = header.ReadBit();

            if (isSaved)
            {
                var key = (int)header.Deserializer.ReadCompressedInt(ref header);
                return header.Deserializer.SavedTypes[key];
            }

            return null;
        }

        public override IABSaveConverterContext TryGenerateContext(ABSaveMap map, Type type)
        {
            if (type == typeof(Type) || type.IsSubclassOf(typeof(Type))) return new Context(map.GetMaptimeSubItem(typeof(Assembly)));
            else return null;
        }

        class Context : IABSaveConverterContext
        {
            public MapItem AssemblyMap;

            public Context(MapItem asmMap) => AssemblyMap = asmMap;
        }
    }
}
