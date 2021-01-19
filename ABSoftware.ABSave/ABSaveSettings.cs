using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;

namespace ABSoftware.ABSave
{
    public enum SettingsPreset
    {
        PrioritizePerformance,
        PrioritizeSize
    }

    public struct ABSaveSettingsBuilder
    {
        public bool? ConvertFields { get; set; }
        public bool? IncludePrivate { get; set; }
        public bool? LazyBitHandling { get; set; }
        public bool? UseUTF8 { get; set; }
        public bool? UseLittleEndian { get; set; }
        public bool? SaveInheritance { get; set; }
        public bool? BypassDangerousTypeChecking { get; set; }
        public List<ABSaveConverter> CustomConverters { get; set; }

        public ABSaveSettings CreateSettings(ABSaveSettings template)
        {
            var convertFields = ConvertFields ?? template.ConvertFields;
            var includePrivate = IncludePrivate ?? template.IncludePrivate;
            var lazyBitHandling = LazyBitHandling ?? template.LazyBitHandling;
            var useUTF8 = UseUTF8 ?? template.UseUTF8;
            var useLittleEndian = UseLittleEndian ?? template.UseLittleEndian;
            var saveInheritance = SaveInheritance ?? template.SaveInheritance;
            var bypassDangerousTypeChecking = BypassDangerousTypeChecking ?? template.BypassDangerousTypeChecking;

            Dictionary<Type, ABSaveConverter> exactConverters = null;
            List<ABSaveConverter> nonExactConverters = null;

            // Set the custom converters correctly.
            if (CustomConverters != null)
            {
                for (int i = 0; i < CustomConverters.Count; i++)
                {
                    var currentConverter = CustomConverters[i];
                    var exactTypes = CustomConverters[i].ExactTypes;

                    if (exactTypes.Length > 0)
                    {
                        exactConverters ??= new Dictionary<Type, ABSaveConverter>(ABSaveConverter.BuiltInExact);
                        exactConverters.EnsureCapacity(exactConverters.Count + exactTypes.Length);

                        for (int j = 0; j < exactTypes.Length; j++)
                            exactConverters.Add(exactTypes[j], currentConverter);
                    }

                    if (currentConverter.AlsoConvertsNonExact)
                    {
                        nonExactConverters ??= new List<ABSaveConverter>(ABSaveConverter.BuiltInNonExact);
                        nonExactConverters.Add(currentConverter);
                    }
                }
            }

            return new ABSaveSettings(convertFields, includePrivate, lazyBitHandling, useUTF8, saveInheritance, bypassDangerousTypeChecking, useLittleEndian,
                exactConverters ?? ABSaveConverter.BuiltInExact, nonExactConverters ?? ABSaveConverter.BuiltInNonExact);
        }
    }

    public enum ABSavePresets
    {
        SpeedFocusInheritance,
        SpeedFocusNoInheritance,
        SizeFocusInheritance,
        SizeFocusNoInheritance
    }

    /// <summary>
    /// Stores the configuration for serialization/deserialization.
    /// </summary>
    public class ABSaveSettings
    {
        static readonly ABSaveSettings _speedFocusedNoInheritance = new ABSaveSettings(false, false, true, true, false, false, BitConverter.IsLittleEndian, ABSaveConverter.BuiltInExact, ABSaveConverter.BuiltInNonExact);
        static readonly ABSaveSettings _speedFocusedInheritance = new ABSaveSettings(false, false, true, true, true, false, BitConverter.IsLittleEndian, ABSaveConverter.BuiltInExact, ABSaveConverter.BuiltInNonExact);
        static readonly ABSaveSettings _sizeFocusedNoInheritance = new ABSaveSettings(false, false, false, true, false, false, BitConverter.IsLittleEndian, ABSaveConverter.BuiltInExact, ABSaveConverter.BuiltInNonExact);
        static readonly ABSaveSettings _sizeFocusedInheritance = new ABSaveSettings(false, false, false, true, true, false, BitConverter.IsLittleEndian, ABSaveConverter.BuiltInExact, ABSaveConverter.BuiltInNonExact);

        public static ABSaveSettings GetPreset(ABSavePresets presets)
        {
            return presets switch
            {
                ABSavePresets.SpeedFocusNoInheritance => _speedFocusedNoInheritance,
                ABSavePresets.SpeedFocusInheritance => _speedFocusedInheritance,
                ABSavePresets.SizeFocusNoInheritance => _sizeFocusedNoInheritance,
                ABSavePresets.SizeFocusInheritance => _sizeFocusedInheritance,
                _ => throw new Exception("Invalid preset given")
            };
        }

        public bool ConvertFields { get; } = false;
        public bool IncludePrivate { get; } = false;
        public bool LazyBitHandling { get; } = true;
        public bool UseUTF8 { get; } = true;
        public bool SaveInheritance { get; set; } = true;
        public bool BypassDangerousTypeChecking { get; set; } = false;
        public bool UseLittleEndian { get; } = BitConverter.IsLittleEndian;

        public IReadOnlyDictionary<Type, ABSaveConverter> ExactConverters { get; } = ABSaveConverter.BuiltInExact;
        public IReadOnlyList<ABSaveConverter> NonExactConverters { get; } = ABSaveConverter.BuiltInNonExact;

        public ABSaveSettings() { }

        public ABSaveSettings(bool convertFields, bool includePrivate, bool lazyBitHandling, bool useUTF8,
            bool enableInheritance, bool bypassDangerousTypeChecking, bool useLittleEndian,
            IReadOnlyDictionary<Type, ABSaveConverter> exactConverters, IReadOnlyList<ABSaveConverter> nonExactConverters)
        =>
            (ConvertFields, IncludePrivate, LazyBitHandling, UseUTF8, UseLittleEndian, BypassDangerousTypeChecking, ExactConverters, NonExactConverters) = 
            (convertFields, includePrivate, lazyBitHandling, useUTF8, useLittleEndian, bypassDangerousTypeChecking, exactConverters, nonExactConverters);
    }
}
