using ABCo.ABSave.Converters;
using ABCo.ABSave.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;

namespace ABCo.ABSave.Configuration
{
    /// <summary>
    /// Stores the configuration for serialization/deserialization.
    /// </summary>
    public class ABSaveSettings
    {
        public static ABSaveSettings ForSpeed { get; } = new ABSaveSettings(true, true, false, true, Converter.BuiltInExact, Converter.BuiltInNonExact);
        public static ABSaveSettings ForSize { get; } = new ABSaveSettings(false, true, false, true, Converter.BuiltInExact, Converter.BuiltInNonExact);

        public bool LazyBitHandling { get; } = true;
        public bool UseUTF8 { get; } = true;
        public bool BypassDangerousTypeChecking { get; set; } = false;
        public bool UseLittleEndian { get; } = true;

        public IReadOnlyDictionary<Type, Converter>? ExactConverters { get; } = Converter.BuiltInExact;
        public IReadOnlyList<Converter>? NonExactConverters { get; } = Converter.BuiltInNonExact;

        public ABSaveSettings() { }

        public ABSaveSettings(bool lazyBitHandling, bool useUTF8, bool bypassDangerousTypeChecking, bool useLittleEndian,
            IReadOnlyDictionary<Type, Converter>? exactConverters, IReadOnlyList<Converter>? nonExactConverters)
        =>
            (LazyBitHandling, UseUTF8, UseLittleEndian, BypassDangerousTypeChecking, ExactConverters, NonExactConverters) = 
            (lazyBitHandling, useUTF8, useLittleEndian, bypassDangerousTypeChecking, exactConverters, nonExactConverters);
    }
}
