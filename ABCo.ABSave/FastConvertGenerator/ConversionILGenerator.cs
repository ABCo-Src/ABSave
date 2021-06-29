namespace ABCo.ABSave.FastConvertGenerator
{
    /// <summary>
    /// Generates a dynamic method for serializing and deserializing an object, and all of its sub-types.
    /// </summary>
    //public static class ConversionILGenerator
    //{
    //    // Serialize constants:
    //    static readonly Type[] SerializeParameters = new Type[] { typeof(object), typeof(ABSaveSerializer) };
    //    static readonly MethodInfo WriteInt32Info = typeof(ABSaveSerializer).GetMethod(nameof(ABSaveSerializer.WriteInt32));
    //    static readonly MethodInfo ConverterSerialize = typeof(ABSaveTypeConverter).GetMethod(nameof(ABSaveTypeConverter.Serialize));

    //    static readonly Type[] DeserializeParameters = new Type[] { typeof(ABSaveDeserializer) };

    //    public static Action<object, ABSaveSerializer> GenerateSerializeForMap(Type type, ABSaveObjectMapItem map)
    //    {
    //        var res = new DynamicMethod("FastSerialize", type, SerializeParameters);
    //        var ilGenerator = res.GetILGenerator();

    //        // Write the size.
    //        ilGenerator.Emit(OpCodes.Ldarg_1);
    //        ilGenerator.Emit(OpCodes.Ldc_I4, map.Fields.Length);
    //        ilGenerator.EmitCall(OpCodes.Call, WriteInt32Info, null);

    //        // Serialize each field.
    //        for (int i = 0; i < map.Fields.Length; i++)
    //        {
    //            if (map.Fields[i].Map is ObjectMapItem objItm)
    //            {
    //                if (map.Fields[i].Map.FastSerializeIL == null) map.Fields[i].Map.FastSerializeIL = GenerateSerializeForMap(map.Fields[i].Info.FieldType, objItm);

    //                // Call the serialize for this type.
    //                ilGenerator.Emit(OpCodes.Ldarg_0);
    //                ilGenerator.Emit(OpCodes.Ldfld, map.Fields[i].Info);

    //                ilGenerator.Emit(OpCodes.Ldarg_1);

    //                ilGenerator.EmitCall(OpCodes.Call, map.Fields[i].Map.FastSerializeIL.Method, null);
    //            }
    //            else if (map.Fields[i].Map is ObjectMapItem convItm)
    //            {
    //                // Call the converter.
    //                var field = convItm.ConverterType.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
    //                if (field == null) throw new Exception("ABSave: A converter was encountered without a static 'Instance' field, ABSave cannot generate fast IL without all converters having this field.");

    //                ilGenerator.Emit(OpCodes.Ldsfld, field);

    //                ilGenerator.Emit(OpCodes.Ldarg_0);
    //                ilGenerator.Emit(OpCodes.Ldfld, map.Fields[i].Info);

    //                ilGenerator.Emit(OpCodes.Ldtoken, map.Fields[i].Info.FieldType);

    //                ilGenerator.Emit(OpCodes.Ldarg_1);

    //                ilGenerator.Emit(OpCodes.Callvirt, ConverterSerialize);
    //            }
    //        }

    //        ilGenerator.Emit(OpCodes.Ret);
    //        return (Action<object, ABSaveSerializer>)res.CreateDelegate(typeof(Action<object, ABSaveSerializer>));
    //    }
    //}
}
