using ABSoftware.ABSave.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave
{
    /// <summary>
    /// Stores the configuration for serialization/deserialization.
    /// </summary>
    public class ABSaveSettings
    {
        public bool CacheTypesAndAssemblies = true;
        public bool AutoCheckTypeConverters = true;
        public bool UseLittleEndian = BitConverter.IsLittleEndian;

        internal Dictionary<Type, ABSaveTypeConverter> ExactConverters;
        internal List<ABSaveTypeConverter> NonExactConverters;

        public ABSaveSettings() 
        {
            ExactConverters = ABSaveTypeConverter.BuiltInExact;
            NonExactConverters = ABSaveTypeConverter.BuiltInNonExact;
        }

        public ABSaveSettings SetCacheTypesAndAssemblies(bool cacheTypesAndAssemblies)
        {
            CacheTypesAndAssemblies = cacheTypesAndAssemblies;
            return this;
        }

        public ABSaveSettings SetUseLittleEndian(bool useLittleEndian)
        {
            UseLittleEndian = useLittleEndian;
            return this;
        }

        public ABSaveSettings SetAutoCheckTypeConverters(bool checkTypeConverters)
        {
            AutoCheckTypeConverters = checkTypeConverters;
            return this;
        }

        public ABSaveSettings AddTypeConverter(ABSaveTypeConverter converter)
        {
            if (converter.HasExactType)
                ExactConverters[converter.ExactType] = converter;
            else
                NonExactConverters.Add(converter);

            return this;
        } 
    }
}
