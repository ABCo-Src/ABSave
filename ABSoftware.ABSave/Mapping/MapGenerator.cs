using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

namespace ABSoftware.ABSave.Mapping
{
    internal static class MapGenerator
    {
        internal static MapItemType GenerateItemType(Type type) => new MapItemType(type, type.IsValueType);

        internal static MapItem Generate(MapItemType typeInfo, ABSaveMap root)
        {
            // Nullable
            if (IsNullable(typeInfo.Type))
                return new NullableMapItem(typeInfo, Generate(GenerateItemType(typeInfo.Type.GetGenericArguments()[0]), root));

            // Converter
            var isConverter = TryFindConverterForType(root, typeInfo.Type, out ABSaveConverter converter, out IABSaveConverterContext context);
            if (isConverter) return new ConverterMapItem(typeInfo, converter, context);

            // Object
            var items = GenerateObjectMap(typeInfo, root);
            return new ObjectMapItem(items, typeInfo);
        }

        #region Object

        static ObjectMemberInfo[] GenerateObjectMap(MapItemType objType, ABSaveMap map)
        {
            if (map.Settings.ConvertFields)
                return GenerateWithFields(objType.Type, map);
            else
                return GenerateWithProperties(objType, map);
        }

        static ObjectMemberInfo[] GenerateWithProperties(MapItemType objType, ABSaveMap map)
        {
            var bindingFlags = GetFlagsForAccessLevel(map.Settings.IncludePrivate);
            var properties = objType.Type.GetProperties(bindingFlags);
            var ordered = properties.Where(p => p.CanRead && p.CanWrite).OrderBy(f => f.Name).ToList();

            var final = new ObjectMemberInfo[ordered.Count];
            for (int i = 0; i < ordered.Count; i++)
            {
                var typeInfo = GenerateItemType(ordered[i].PropertyType);
                var either = new Either<FieldInfo, PropertyMapInfo>();

                // Put the correct getter and setter for the type.
                either.Right.Getter = GenerateFastPropertyGetter(objType, typeInfo, ordered[i].GetGetMethod());
                either.Right.Setter = GenerateFastPropertySetter(objType, typeInfo, ordered[i].GetSetMethod());

                final[i].Info = either;
                AdjustMember(ref final[i], typeInfo, map);
            }

            return final;
        }

        static ObjectMemberInfo[] GenerateWithFields(Type objType, ABSaveMap map)
        {
            var bindingFlags = GetFlagsForAccessLevel(map.Settings.IncludePrivate);
            var fields = objType.GetFields(bindingFlags);
            var ordered = fields.OrderBy(f => f.Name).ToList();

            var final = new ObjectMemberInfo[ordered.Count];

            for (int i = 0; i < ordered.Count; i++)
            {
                final[i].Info = new Either<FieldInfo, PropertyMapInfo>(ordered[i]);
                AdjustMember(ref final[i], GenerateItemType(ordered[i].FieldType), map);
            }    

            return final;
        }

        static void AdjustMember(ref ObjectMemberInfo member, MapItemType itemType, ABSaveMap map)
        {
            member.Map = Generate(itemType, map);
        }

        #endregion

        #region Fast Property Access

        static readonly Type[] FastGetterParams = new Type[] { typeof(object) };
        static readonly Type[] FastSetterParams = new Type[] { typeof(object), typeof(object) };

        static Func<object, object> GenerateFastPropertyGetter(MapItemType parentType, MapItemType propertyType, MethodInfo info)
        {
            if (RuntimeFeature.IsDynamicCodeSupported)
            {
                var dynMethod = new DynamicMethod("ABSaveFastPropertyGetter", typeof(object), FastGetterParams);
                var ilGenerator = dynMethod.GetILGenerator();

                EmitLoadParent(parentType, ilGenerator);
                ilGenerator.EmitCall(OpCodes.Callvirt, info, null);

                if (propertyType.IsValueType) ilGenerator.Emit(OpCodes.Box, propertyType.Type);
                ilGenerator.Emit(OpCodes.Ret);

                return (Func<object, object>)dynMethod.CreateDelegate(typeof(Func<object, object>));
            }

            else return (v) => info.Invoke(v, null);
        }

        static Action<object, object> GenerateFastPropertySetter(MapItemType parentType, MapItemType propertyType, MethodInfo info)
        {
            if (RuntimeFeature.IsDynamicCodeSupported)
            {
                var dynMethod = new DynamicMethod("ABSaveFastPropertySetter", null, FastSetterParams);
                var ilGenerator = dynMethod.GetILGenerator();

                EmitLoadParent(parentType, ilGenerator);

                ilGenerator.Emit(OpCodes.Ldarg_1);
                if (propertyType.IsValueType) ilGenerator.Emit(OpCodes.Unbox_Any, propertyType.Type);

                ilGenerator.EmitCall(OpCodes.Callvirt, info, null);
                ilGenerator.Emit(OpCodes.Ret);

                return (Action<object, object>)dynMethod.CreateDelegate(typeof(Action<object, object>));
            }

            else return (o, v) => info.Invoke(o, new object[] { v }); // TODO: Optimize array allocation
        }

        static void EmitLoadParent(MapItemType parentType, ILGenerator gen)
        {
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(parentType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, parentType.Type);            
        }

        #endregion

        #region Helpers

        static bool TryFindConverterForType(ABSaveMap map, Type type, out ABSaveConverter converter, out IABSaveConverterContext context)
        {
            var settings = map.Settings;
            if (settings.ExactConverters.TryGetValue(type, out ABSaveConverter val))
            {
                converter = val;
                context = val.TryGenerateContext(map, type);
                return true;
            }
            else
                for (int i = settings.NonExactConverters.Count - 1; i >= 0; i--)
                {
                    context = settings.NonExactConverters[i].TryGenerateContext(map, type);

                    if (context != null)
                    {
                        converter = settings.NonExactConverters[i];
                        return true;
                    }
                }

            context = null;
            converter = null;
            return false;
        }

        static bool IsNullable(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

        static BindingFlags GetFlagsForAccessLevel(bool includePrivate) =>
            includePrivate ? BindingFlags.Public | BindingFlags.Instance : BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        #endregion
    }
}