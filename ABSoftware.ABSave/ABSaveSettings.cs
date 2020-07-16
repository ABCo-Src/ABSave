﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave
{
    /// <summary>
    /// Stores the configuration and general information required for serialization.
    /// Should only be used once per serialization.
    /// </summary>
    public class ABSaveSettings
    {
        public bool WithNames = true;
        public bool WithTypes = true;
        public bool CacheTypesAndAssemblies = true;
        public bool AutoCheckStringConverters = true;
        public bool AutoCheckTypeConverters = true;
        public bool UseLittleEndian = BitConverter.IsLittleEndian;

        public ABSaveSettings() { }

        public ABSaveSettings SetWithNames(bool withNames)
        {
            WithNames = withNames;
            return this;
        }

        public ABSaveSettings SetWithTypes(bool withTypes) 
        {
            WithTypes = withTypes;
            return this;
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

        public ABSaveSettings SetAutoCheckStringConverters(bool checkStringConverters)
        {
            AutoCheckStringConverters = checkStringConverters;
            return this;
        }

        public ABSaveSettings SetAutoCheckTypeConverters(bool checkTypeConverters)
        {
            AutoCheckTypeConverters = checkTypeConverters;
            return this;
        }
    }
}
