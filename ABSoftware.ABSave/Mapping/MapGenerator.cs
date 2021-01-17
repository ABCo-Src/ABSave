using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave.Mapping
{
    internal static class MapGenerator
    {
        internal static MapItem Generate(Type type, ABSaveMap root)
        {
            // Nullable
            if (IsNullable(type))
                return new NullableMapItem(type, Generate(type.GetGenericArguments()[0], root));

            // Converter
            var isConverter = TryFindConverterForType(root, type, out ABSaveConverter converter, out IABSaveConverterContext context);
            if (isConverter) return new ConverterMapItem(type, converter, context);

            // Object
            return GenerateObjectMap(type, root);
        }

        #region Object

        static MapItem GenerateObjectMap(Type objType, ABSaveMap map)
        {
            if (map.Settings.ConvertFields)
                return GenerateWithFields(objType, map);
            else
                return GenerateWithProperties(objType, map);
        }

        static MapItem GenerateWithProperties(Type objType, ABSaveMap map)
        {
            var bindingFlags = GetFlagsForAccessLevel(map.Settings.IncludePrivate);
            var properties = objType.GetProperties(bindingFlags);
            var ordered = properties.Where(p => p.CanRead && p.CanWrite).OrderBy(f => f.Name).ToList();

            var final = new ObjectFieldInfo[ordered.Count];
            for (int i = 0; i < ordered.Count; i++)
                final[i] = PrepareMemberForObject(new Either<PropertyInfo, FieldInfo>(ordered[i]), ordered[i].PropertyType, map);

            return new ObjectMapItem(final, objType);
        }

        static MapItem GenerateWithFields(Type objType, ABSaveMap map)
        {
            var bindingFlags = GetFlagsForAccessLevel(map.Settings.IncludePrivate);
            var fields = objType.GetFields(bindingFlags);
            var ordered = fields.OrderBy(f => f.Name).ToList();

            var final = new ObjectFieldInfo[ordered.Count];
            for (int i = 0; i < ordered.Count; i++)
                final[i] = PrepareMemberForObject(new Either<PropertyInfo, FieldInfo>(ordered[i]), ordered[i].FieldType, map);

            return new ObjectMapItem(final, objType);
        }

        static ObjectFieldInfo PrepareMemberForObject(Either<PropertyInfo, FieldInfo> member, Type itemType, ABSaveMap map)
        {
            return new ObjectFieldInfo
            {
                Info = member,
                Map = Generate(itemType, map)
            };
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