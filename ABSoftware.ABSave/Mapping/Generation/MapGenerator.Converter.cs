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
                    var converter = GetConverterInstance(settings.NonExactConverters[i]);

                    if (converter.CheckType(new CheckTypeInfo(type, settings)))
                        return InitializeConverter(converter, type);
                }
            }

            return null;
        }

        internal MapItem InitializeConverter(Converter converter, Type type)
        {
            ApplyItem(converter, type);
            converter.Initialize(new InitializeInfo(type, this));
            converter._allInheritanceAttributes = GetInheritanceAttributes(type);
            return converter;
        }

        Converter GetConverterInstance(ConverterInfo info) => (Converter)Activator.CreateInstance(info.ConverterType)!;

        // Temporary helper until object conversion gets moved into its own converter
        internal static SaveInheritanceAttribute? GetConverterInheritanceInfoForVersion(uint version, Converter converter)
        {
            // Try to get it from the dictionary cache.
            if (converter._inheritanceValues != null && 
                converter._inheritanceValues.TryGetValue(version, out SaveInheritanceAttribute? res))
                return res;

            // If it's not in there, find it in the attribute array.
            if (converter._allInheritanceAttributes == null) return null;
            SaveInheritanceAttribute? attribute = FindInheritanceAttributeForVersion(converter._allInheritanceAttributes, version);

            converter._inheritanceValues ??= new Dictionary<uint, SaveInheritanceAttribute?>();
            converter._inheritanceValues.Add(version, attribute);
            return attribute;
        }
    }
}
