using ABCo.ABSave.Configuration;
using ABCo.ABSave.Converters;
using ABCo.ABSave.Mapping.Description.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Mapping.Generation
{
    public partial class MapGenerator
    {
        internal MapItem? TryGenerateConverter(Type type)
        {
            var settings = Map.Settings;

            // Exact converter
            if (settings.ExactConverters.TryGetValue(type, out var currentConverter))
                return InitializeConverter(GetConverterInstance(currentConverter), type);

            // Non-exact converter
            if (settings.NonExactConverters != null)
            {
                for (int i = settings.NonExactConverters.Count - 1; i >= 0; i--)
                {
                    var converter = GetConverterInstance(currentConverter);

                    if (converter.CheckType(type))
                        return InitializeConverter(converter, type);
                }
            }

            return null;
        }

        internal MapItem InitializeConverter(Converter converter, Type type)
        {
            converter.Initialize(new InitializeInfo(type, this));
            converter._allInheritanceAttributes = GetInheritanceAttributes(type);
            return converter;
        }

        Converter GetConverterInstance(ConverterInfo info) => (Converter)Activator.CreateInstance(info.ConverterType);

        // Temporary helper until object conversion gets moved into its own converter
        internal static SaveInheritanceAttribute? GetConverterInheritanceInfoForVersion(uint version, Converter converter)
        {
            // Try to get it from the dictionary cache.
            SaveInheritanceAttribute? cached = converter._inheritanceValues?.GetValueOrDefault(version);
            if (cached != null) return cached;

            // If it's not in there, find it in the attribute array.
            if (converter._allInheritanceAttributes == null) return null;
            SaveInheritanceAttribute? attribute = FindInheritanceAttributeForVersion(converter._allInheritanceAttributes, version);

            converter._inheritanceValues ??= new Dictionary<uint, SaveInheritanceAttribute?>();
            converter._inheritanceValues.Add(version, attribute);
            return attribute;
        }
    }
}
