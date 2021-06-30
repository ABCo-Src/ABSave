using ABCo.ABSave.Mapping.Generation.Converters;
using System;
using System.Collections.Generic;

namespace ABCo.ABSave.Configuration
{
    public class SettingsBuilder
    {
        bool? _lazyWriteCompressed;
        bool? _lazyBitHandling;
        bool? _useUTF8;
        bool? _useLittleEndian;
        bool? _bypassDangerousTypeChecking;

        List<ConverterInfo>? _converters;

        public SettingsBuilder SetLazyBitHandling(bool lazyBitHandling)
        {
            _lazyBitHandling = lazyBitHandling;
            return this;
        }

        public SettingsBuilder SetUseUTF8(bool useUTF8)
        {
            _useUTF8 = useUTF8;
            return this;
        }

        public SettingsBuilder SetUseLittleEndian(bool useLittleEndian)
        {
            _useLittleEndian = useLittleEndian;
            return this;
        }

        public SettingsBuilder SetBypassDangerousTypeChecking(bool bypassDangerousTypeChecking)
        {
            _bypassDangerousTypeChecking = bypassDangerousTypeChecking;
            return this;
        }

        internal SettingsBuilder SetLazyWriteCompressed(bool lazyWriteCompressed)
        {
            _lazyWriteCompressed = lazyWriteCompressed;
            return this;
        }

        internal ABSaveSettings CreateSettings(ABSaveSettings template)
        {
            // Handle basic settings
            bool lazyWriteCompressed = _lazyWriteCompressed ?? template.LazyCompressedWriting;
            bool lazyBitHandling = _lazyBitHandling ?? template.LazyBitHandling;
            bool useUTF8 = _useUTF8 ?? template.UseUTF8;
            bool useLittleEndian = _useLittleEndian ?? template.UseLittleEndian;

            // Process converters
            EnsureConvertersListInitialized();
            SettingsConverterProcessor.Split(_converters!, out IReadOnlyDictionary<Type, ConverterInfo>? exactConverters, out IReadOnlyList<ConverterInfo>? nonExactConverter);

            // Create the new settings.
            return new ABSaveSettings(lazyWriteCompressed, lazyBitHandling, useUTF8, useLittleEndian,
                _converters!.Count, exactConverters, nonExactConverter);
        }

        public SettingsBuilder AddConverter<T>() => AddConverterNonGeneric(typeof(T));
        public SettingsBuilder AddConverterNonGeneric(Type type)
        {
            EnsureConvertersListInitialized();
            _converters!.Add(new ConverterInfo(type, _converters.Count));

            return this;
        }

        void EnsureConvertersListInitialized() =>
            _converters ??= new List<ConverterInfo>(BuiltInConverters.Infos);
    }
}
