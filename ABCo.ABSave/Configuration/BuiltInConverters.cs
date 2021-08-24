using ABCo.ABSave.Serialization.Converters;

namespace ABCo.ABSave.Configuration
{
    internal class BuiltInConverters
    {
        internal static ConverterInfo[] Infos { get; } = new ConverterInfo[]
        {
            new ConverterInfo(typeof(KeyValueConverter), 0),
            new ConverterInfo(typeof(CollectionConverter), 1),
            new ConverterInfo(typeof(ArrayConverter), 2),
            new ConverterInfo(typeof(PrimitiveConverter), 3),
            new ConverterInfo(typeof(GuidConverter), 4),
            new ConverterInfo(typeof(TextConverter), 5),
            new ConverterInfo(typeof(TickBasedConverter), 6),
            new ConverterInfo(typeof(VersionConverter), 7),
            new ConverterInfo(typeof(ObjectConverter), 8)
        };
    }
}
