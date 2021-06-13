using ABCo.ABSave.Converters;
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

        List<ConverterInfo>? _converters;

        public ABSaveSettings CreateSettings(ABSaveSettings template)
        {
            EnsureConvertersListInitialized();

            // Handle basic settings
            var lazyBitHandling = LazyBitHandling ?? template.LazyBitHandling;
            var useUTF8 = UseUTF8 ?? template.UseUTF8;
            var useLittleEndian = UseLittleEndian ?? template.UseLittleEndian;
            var bypassDangerousTypeChecking = BypassDangerousTypeChecking ?? template.BypassDangerousTypeChecking;

            Dictionary<Type, ConverterInfo> exactConverters = new Dictionary<Type, ConverterInfo>();
            List<ConverterInfo> nonExactConverters = new List<ConverterInfo>();

            // Add custom converters.
            if (_converters != null)
            {
                for (int i = 0; i < _converters.Count; i++)
                {
                    var currentConverter = _converters[i];

                    // TODO: Use attributes instead of this - see issue #17.
                    var tempInstance = (Converter)Activator.CreateInstance(_converters[i].ConverterType);
                    var exactTypes = tempInstance.ExactTypes;

                    if (exactTypes.Length > 0)
                    {
                        exactConverters ??= new Dictionary<Type, ConverterInfo>();
                        exactConverters.EnsureCapacity(exactConverters.Count + exactTypes.Length);

                        for (int j = 0; j < exactTypes.Length; j++)
                            exactConverters.Add(exactTypes[j], currentConverter);
                    }

                    if (tempInstance.AlsoConvertsNonExact)
                    {
                        nonExactConverters ??= new List<ConverterInfo>();
                        nonExactConverters.Add(currentConverter);
                    }
                }
            }

            return new ABSaveSettings(lazyBitHandling, useUTF8, bypassDangerousTypeChecking, useLittleEndian,
                exactConverters, nonExactConverters);
        }

        public void AddConverter(Type type)
        {
            EnsureConvertersListInitialized();
            _converters!.Add(new ConverterInfo(type, _converters.Count));
        }

        void EnsureConvertersListInitialized() =>
            _converters ??= new List<ConverterInfo>(BuiltInConverters.Infos);
    }
}
