using ABCo.ABSave.Mapping.Generation.Converters;
using System;
using System.Collections.Generic;

namespace ABCo.ABSave.Configuration
{
    /// <summary>
    /// Stores the configuration for serialization/deserialization.
    /// </summary>
    public class ABSaveSettings
    {
        public static ABSaveSettings ForSpeed { get; }
        public static ABSaveSettings ForSize { get; }

        static ABSaveSettings()
        {
            SettingsConverterProcessor.Split(BuiltInConverters.Infos, out IReadOnlyDictionary<Type, ConverterInfo>? exactConverters, out IReadOnlyList<ConverterInfo>? nonExactConverters);

            ForSpeed = new ABSaveSettings(true, true, true, true, BuiltInConverters.Infos.Length, exactConverters, nonExactConverters);
            ForSize = new ABSaveSettings(false, true, true, true, BuiltInConverters.Infos.Length, exactConverters, nonExactConverters);
        }

        public bool LazyCompressedWriting { get; }
        public bool IncludeVersioning { get; }
        public bool UseUTF8 { get; }
        public bool UseLittleEndian { get; }

        internal IReadOnlyDictionary<Type, ConverterInfo> ExactConverters { get; }
        internal IReadOnlyList<ConverterInfo> NonExactConverters { get; }
        internal int ConverterCount { get; }

        internal ABSaveSettings Customize(Action<SettingsBuilder> customizer)
        {
            var builder = new SettingsBuilder();
            customizer(builder);

            return builder.CreateSettings(this);
        }

        internal ABSaveSettings(bool lazyCompressedWriting, bool useUTF8, bool useLittleEndian, bool includeVersioning, int converterCount,
            IReadOnlyDictionary<Type, ConverterInfo> exactConverters, IReadOnlyList<ConverterInfo> nonExactConverters)
        =>
            (LazyCompressedWriting, UseUTF8, UseLittleEndian, IncludeVersioning, ConverterCount, ExactConverters, NonExactConverters) =
            (lazyCompressedWriting, useUTF8, useLittleEndian, includeVersioning, converterCount, exactConverters, nonExactConverters);
    }
}
