﻿using ABCo.ABSave.Configuration;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ABCo.ABSave.Mapping.Generation
{
    /// <summary>
    /// Takes a list and organises them into non-exact and exact converters that can then
    /// be placed into a settings object.
    /// </summary>
    internal static class SettingsConverterProcessor
    {
        public static void Split(
            IList<ConverterInfo> converters,
            out IReadOnlyDictionary<Type, ConverterInfo> outExactConverters,
            out IReadOnlyList<ConverterInfo> outNonExactConverters)
        {
            Dictionary<Type, ConverterInfo>? exactConverters = new Dictionary<Type, ConverterInfo>();
            List<ConverterInfo> nonExactConverters = new List<ConverterInfo>();

            for (int i = 0; i < converters.Count; i++)
            {
                var currentConverter = converters[i];
                var currentConverterType = currentConverter.ConverterType;

                var exactTypes =
                    (SelectAttribute[])currentConverterType.GetCustomAttributes<SelectAttribute>(false);
                var alsoNeedsCheckType =
                    Attribute.IsDefined(currentConverterType, typeof(SelectOtherWithCheckTypeAttribute));

                if (exactTypes.Length > 0)
                {
                    exactConverters.EnsureCapacity(exactConverters.Count + exactTypes.Length);

                    for (int j = 0; j < exactTypes.Length; j++)
                        exactConverters.Add(exactTypes[j].Type, currentConverter);
                }

                if (alsoNeedsCheckType)
                    nonExactConverters.Add(currentConverter);                
            }
             
            outExactConverters = exactConverters;
            outNonExactConverters = nonExactConverters;
        }
    }
}
