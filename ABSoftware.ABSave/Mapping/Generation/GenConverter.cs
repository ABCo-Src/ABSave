using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Mapping.Generation
{
    internal static class GenConverter
    {
        internal static bool TryGenerateConvert(Type type, MapGenerator gen, MapItemInfo dest)
        {
            var genContext = new ContextGen(type, gen);

            if (!TryGetConverter(ref genContext, out var converter, out var context))
                return false;

            // Place the converter and its context into the item. 
            ref MapItem item = ref gen.FillItemWith(MapItemType.Converter, dest);
            ref ConverterMapItem convInfo = ref MapItem.GetConverterData(ref item);

            (convInfo.Converter, convInfo.Context) = (converter, context);
            item.IsGenerating = false;
            return true;
        }

        static bool TryGetConverter(ref ContextGen gen, out Converter converter, out IConverterContext context)
        {
            var settings = gen.Settings;

            // Exact converter
            if (settings.ExactConverters.TryGetValue(gen.Type, out converter))
            {
                context = converter.TryGenerateContext(ref gen);

                if (!gen._marked) throw new Exception("Converter refused to convert one of its exact types when asked to generate a context.");
                return true;
            }

            // Non-exact converter
            for (int i = settings.NonExactConverters.Count - 1; i >= 0; i--)
            {
                context = settings.NonExactConverters[i].TryGenerateContext(ref gen);

                if (gen._marked)
                {
                    converter = settings.NonExactConverters[i];
                    return true;
                }
            }

            (converter, context) = (null, null);
            return false;
        }
    }

    public struct ContextGen
    {
        public Type Type;
        readonly MapGenerator _gen;
        internal bool _marked;

        public ABSaveMap Map => _gen.Map;
        public ABSaveSettings Settings => _gen.Map.Settings;

        public void MarkCanConvert() => _marked = true;

        /// <summary>
        /// Gets the map for the given type.
        /// </summary>
        public MapItemInfo GetMap(Type type) => _gen.GetMap(type);

        internal ContextGen(Type type, MapGenerator gen) => 
            (Type, _gen, _marked) = (type, gen, false);
    }
}
