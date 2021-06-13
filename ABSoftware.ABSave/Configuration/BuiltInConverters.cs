using ABCo.ABSave.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Configuration
{
    internal class BuiltInConverters
    {
        internal static ConverterInfo[] Infos { get; } = new ConverterInfo[]
        {
            new ConverterInfo(typeof(ArrayConverter), 0),
            new ConverterInfo(typeof(AssemblyConverter), 1),
            new ConverterInfo(typeof(EnumerableConverter), 2),
            new ConverterInfo(typeof(GuidConverter), 3),
            new ConverterInfo(typeof(KeyValueConverter), 4),
            new ConverterInfo(typeof(PrimitiveConverter), 5),
            new ConverterInfo(typeof(TextConverter), 6),
            new ConverterInfo(typeof(TickBasedConverter), 7),
            new ConverterInfo(typeof(TypeConverter), 8),
            new ConverterInfo(typeof(VersionConverter), 9)
        };
    }
}
