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
        public List<Converter> CustomConverters { get; set; }

        public ABSaveSettings CreateSettings(ABSaveSettings template)
        {
            var convertFields = ConvertFields ?? template.ConvertFields;
            var includePrivate = IncludePrivate ?? template.IncludePrivate;
            var lazyBitHandling = LazyBitHandling ?? template.LazyBitHandling;
            var useUTF8 = UseUTF8 ?? template.UseUTF8;
            var useLittleEndian = UseLittleEndian ?? template.UseLittleEndian;
            var saveInheritance = SaveInheritance ?? template.SaveInheritance;
            var bypassDangerousTypeChecking = BypassDangerousTypeChecking ?? template.BypassDangerousTypeChecking;

            Dictionary<Type, Converter> exactConverters = null;
            List<Converter> nonExactConverters = null;

            // Set the custom converters correctly.
            if (CustomConverters != null)
            {
                for (int i = 0; i < CustomConverters.Count; i++)
                {
                    var currentConverter = CustomConverters[i];
                    var exactTypes = CustomConverters[i].ExactTypes;

                    if (exactTypes.Length > 0)
                    {
                        exactConverters ??= new Dictionary<Type, Converter>(Converter.BuiltInExact);
                        exactConverters.EnsureCapacity(exactConverters.Count + exactTypes.Length);

                        for (int j = 0; j < exactTypes.Length; j++)
                            exactConverters.Add(exactTypes[j], currentConverter);
                    }

                    if (currentConverter.AlsoConvertsNonExact)
                    {
                        nonExactConverters ??= new List<Converter>(Converter.BuiltInNonExact);
                        nonExactConverters.Add(currentConverter);
                    }
                }
            }

            return new ABSaveSettings(convertFields, includePrivate, lazyBitHandling, useUTF8, saveInheritance, bypassDangerousTypeChecking, useLittleEndian,
                exactConverters ?? Converter.BuiltInExact, nonExactConverters ?? Converter.BuiltInNonExact);
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
        static readonly ABSaveSettings _speedFocusedNoInheritance = new ABSaveSettings(false, false, true, true, false, false, BitConverter.IsLittleEndian, Converter.BuiltInExact, Converter.BuiltInNonExact);
        static readonly ABSaveSettings _speedFocusedInheritance = new ABSaveSettings(false, false, true, true, true, false, BitConverter.IsLittleEndian, Converter.BuiltInExact, Converter.BuiltInNonExact);
        static readonly ABSaveSettings _sizeFocusedNoInheritance = new ABSaveSettings(false, false, false, true, false, false, BitConverter.IsLittleEndian, Converter.BuiltInExact, Converter.BuiltInNonExact);
        static readonly ABSaveSettings _sizeFocusedInheritance = new ABSaveSettings(false, false, false, true, true, false, BitConverter.IsLittleEndian, Converter.BuiltInExact, Converter.BuiltInNonExact);

        public static ABSaveSettings GetSpeedFocus(bool inheritanceEnabled)
        {
            if (inheritanceEnabled)
                return _speedFocusedInheritance;
            else
                return _speedFocusedNoInheritance;
        }

        public static ABSaveSettings GetSizeFocus(bool inheritanceEnabled)
        {
            if (inheritanceEnabled)
                return _sizeFocusedInheritance;
            else
                return _sizeFocusedNoInheritance;
        }

        public bool ConvertFields { get; } = false;
        public bool IncludePrivate { get; } = false;
        public bool LazyBitHandling { get; } = true;
        public bool UseUTF8 { get; } = true;
        public bool SaveInheritance { get; set; } = true;
        public bool BypassDangerousTypeChecking { get; set; } = false;
        public bool UseLittleEndian { get; } = BitConverter.IsLittleEndian;

        public IReadOnlyDictionary<Type, Converter> ExactConverters { get; } = Converter.BuiltInExact;
        public IReadOnlyList<Converter> NonExactConverters { get; } = Converter.BuiltInNonExact;

        public ABSaveSettings() { }

        public ABSaveSettings(bool convertFields, bool includePrivate, bool lazyBitHandling, bool useUTF8,
            bool saveInheritance, bool bypassDangerousTypeChecking, bool useLittleEndian,
            IReadOnlyDictionary<Type, Converter> exactConverters, IReadOnlyList<Converter> nonExactConverters)
        =>
            (ConvertFields, IncludePrivate, LazyBitHandling, UseUTF8, UseLittleEndian, SaveInheritance, BypassDangerousTypeChecking, ExactConverters, NonExactConverters) = 
            (convertFields, includePrivate, lazyBitHandling, useUTF8, useLittleEndian, saveInheritance, bypassDangerousTypeChecking, exactConverters, nonExactConverters);
    }
}
