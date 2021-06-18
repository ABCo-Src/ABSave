using ABCo.ABSave.Deserialization;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Mapping.Generation;
using ABCo.ABSave.Serialization;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace ABCo.ABSave.Converters
{
    //[Select(typeof(Type), typeof(Assembly))]
    //[SelectOtherWithCheckType]
    //public class TypeConverter : Converter
    //{
    //    public override void Serialize(object obj, Type actualType, ref BitTarget header) => SerializeType((Type)obj, ref header);

    //    static void SerializeType(Type type, ref BitTarget header)
    //    {
    //        // Convert the type
    //        if (type.IsGenericType)
    //        {
    //            header.WriteBitWith(type.IsGenericTypeDefinition);

    //            SerializeTypeMainPart(type.GetGenericTypeDefinition(), ref header);

    //            if (!type.IsGenericTypeDefinition)
    //                SerializeGenerics(type, header.Serializer);
    //        }
    //        else
    //        {
    //            if (type.IsGenericTypeParameter)
    //                throw new Exception("ABSave does not currently support serializing open generic parameters within partially closed types.");

    //            header.WriteBitOff(); // Not generic parameter
    //            SerializeTypeMainPart(type, ref header);
    //        }
    //    }

    //    static void SerializeTypeMainPart(Type type, ref BitTarget header)
    //    {
    //        header.Serializer.SerializeExactNonNullItem(type.Assembly, header.Serializer.Map.AssemblyItem, ref header);
    //        header.Serializer.WriteString(type.FullName);
    //    }

    //    static void SerializeGenerics(Type type, ABSaveSerializer serializer)
    //    {
    //        var generics = type.GetGenericArguments();
    //        var currentTarget = new BitTarget(serializer);

    //        for (int i = 0; i < generics.Length; i++)
    //            SerializeType(generics[i], ref currentTarget);
    //    }

    //    public override object Deserialize(Type actualType, ref BitSource header) => DeserializeType(ref header);

    //    static Type DeserializeType(ref BitSource header)
    //    {

    //        // Convert the type
    //        var mainPart = DeserializeTypeMainPart(ref header);
    //        var withGenerics = mainPart.IsGenericType ? DeserializeGenerics(mainPart, header.Deserializer) : mainPart;

    //        // Save the type
    //        header.Deserializer.SavedTypes.Add(withGenerics);
    //        return withGenerics;
    //    }

    //    public static Type DeserializeTypeMainPart(ref BitSource header)
    //    {
    //        var assembly = (Assembly)header.Deserializer.DeserializeExactNonNullItem(header.Deserializer.Map.AssemblyItem, ref header)!;
    //        var typeName = header.Deserializer.ReadString();

    //        return assembly.GetType(typeName)!;
    //    }

    //    Type DeserializeGenerics(Type mainPart, ABSaveDeserializer deserializer)
    //    {
    //        var parameters = mainPart.GetGenericArguments();
    //        var currentSource = new BitSource(deserializer);

    //        for (int i = 0; i < parameters.Length; i++)
    //            if (currentSource.ReadBit())
    //                parameters[i] = DeserializeType(ref currentSource);
            
    //        return mainPart.MakeGenericType(parameters);
    //    }

    //    static bool HandleSerializeSaved(Type type, ref BitTarget header)
    //    {
    //        var isSaved = header.Serializer.SavedTypes.TryGetValue(type, out int key);

    //        if (isSaved)
    //        {
    //            header.WriteBitOn();
    //            header.Serializer.WriteCompressed((uint)key, ref header);
    //            return true;
    //        }

    //        header.WriteBitOff();
    //        header.Serializer.SavedTypes.Add(type, header.Serializer.SavedTypes.Count);
    //        return false;
    //    }

    //    public override bool CheckType(CheckTypeInfo info) => info.Type == typeof(Type) || info.Type.IsSubclassOf(typeof(Type));

    //    public override bool UsesHeaderForVersion(uint version) => true;
    //}
}
