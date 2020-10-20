using ABSoftware.ABSave.Converters;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave
{
    public enum TextMode
    {
        UTF8 = 0,
        NullTerminatedUTF8 = 1,
        UTF16 = 2,
    }

    /// <summary>
    /// Stores the configuration for serialization/deserialization.
    /// </summary>
    public class ABSaveSettings
    {
        public bool CacheTypesAndAssemblies = true;
        public bool AutoCheckTypeConverters = true;
        public bool ErrorOnUnknownItem = true;
        public TextMode TextMode = TextMode.UTF16;
        public bool UseLittleEndian = BitConverter.IsLittleEndian;
        public BindingFlags MemberReflectionFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private bool _exactConvertersAreCached = true; // Whether the "ExactConverters" is the readonly cached version from "ABSaveTypeConverter".
        internal Dictionary<Type, ABSaveTypeConverter> ExactConverters;

        private bool _nonExactConvertersAreCached = true; // Whether the "NonExactConverters" is the readonly cached version from "ABSaveTypeConverter".
        internal List<ABSaveTypeConverter> NonExactConverters;

        public static ABSaveSettings PrioritizePerformance => new ABSaveSettings();

        public static ABSaveSettings PrioritizeSize => new ABSaveSettings()
        {
            TextMode = TextMode.NullTerminatedUTF8
        };

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

        public ABSaveSettings SetErrorOnUnknownItem(bool errorOnUnknown)
        {
            ErrorOnUnknownItem = errorOnUnknown;
            return this;
        }

        public ABSaveSettings SetTextMode(TextMode textMode)
        {
            TextMode = textMode;
            return this;
        }

        public ABSaveSettings SetReflectionFlags(BindingFlags flags)
        {
            MemberReflectionFlags = flags;
            return this;
        }

        public ABSaveSettings AddTypeConverter(ABSaveTypeConverter converter)
        {
            var exactTypes = converter.ExactTypes;

            if (exactTypes.Length > 0)
            {
                EnsureNotCachedExactConverters();

                ExactConverters.EnsureCapacity(ExactConverters.Count + exactTypes.Length);
                for (int i = 0; i < exactTypes.Length; i++)
                    ExactConverters.Add(exactTypes[i], converter);
            }

            if (converter.HasNonExactTypes)
            {
                EnsureNotCachedNonExactConverters();

                NonExactConverters.Add(converter);
            }

            return this;
        }

        private void EnsureNotCachedExactConverters()
        {
            if (_exactConvertersAreCached)
            {
                ExactConverters = new Dictionary<Type, ABSaveTypeConverter>(ExactConverters);
                _exactConvertersAreCached = false;
            }
        }

        private void EnsureNotCachedNonExactConverters()
        {
            if (_nonExactConvertersAreCached)
            {
                ExactConverters = new Dictionary<Type, ABSaveTypeConverter>(ExactConverters);
                _exactConvertersAreCached = false;
            }
        }
    }
}
