using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Configuration
{
    public struct SettingsBuilder
    {
        public bool? LazyBitHandling { get; set; }
        public bool? UseUTF8 { get; set; }
        public bool? UseLittleEndian { get; set; }
        public bool? BypassDangerousTypeChecking { get; set; }
        public List<Converter> CustomConverters { get; set; }

        public ABSaveSettings CreateSettings(ABSaveSettings template)
        {
            var lazyBitHandling = LazyBitHandling ?? template.LazyBitHandling;
            var useUTF8 = UseUTF8 ?? template.UseUTF8;
            var useLittleEndian = UseLittleEndian ?? template.UseLittleEndian;
            var bypassDangerousTypeChecking = BypassDangerousTypeChecking ?? template.BypassDangerousTypeChecking;

            Dictionary<Type, Converter>? exactConverters = null;
            List<Converter>? nonExactConverters = null;

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

            return new ABSaveSettings(lazyBitHandling, useUTF8, bypassDangerousTypeChecking, useLittleEndian,
                exactConverters ?? Converter.BuiltInExact, nonExactConverters ?? Converter.BuiltInNonExact);
        }
    }
}
