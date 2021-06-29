using ABCo.ABSave.Mapping.Generation;
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

            // (The converter info is filled in by the builder so keep it blank)
            ForSpeed = new ABSaveSettings(true, true, false, true, BuiltInConverters.Infos.Length, exactConverters, nonExactConverters);
            ForSize = new ABSaveSettings(false, true, false, true, BuiltInConverters.Infos.Length, exactConverters, nonExactConverters);
        }

        public bool LazyBitHandling { get; }
        public bool UseUTF8 { get; }
        public bool BypassDangerousTypeChecking { get; }
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

        internal ABSaveSettings(bool lazyBitHandling, bool useUTF8, bool bypassDangerousTypeChecking, bool useLittleEndian, int converterCount,
            IReadOnlyDictionary<Type, ConverterInfo> exactConverters, IReadOnlyList<ConverterInfo> nonExactConverters)
        =>
            (LazyBitHandling, UseUTF8, UseLittleEndian, BypassDangerousTypeChecking, ConverterCount, ExactConverters, NonExactConverters) =
            (lazyBitHandling, useUTF8, useLittleEndian, bypassDangerousTypeChecking, converterCount, exactConverters, nonExactConverters);
    }
}
