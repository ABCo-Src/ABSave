using ABCo.ABSave.Configuration;
using ABCo.ABSave.Converters;
using ABCo.ABSave.UnitTests.TestHelpers;

namespace ABCo.ABSave.UnitTests.Converters
{
    public class ConverterTestBase : TestBase
    {
        public Converter CurrentConverter = null!;

        public void Setup<T>(ABSaveSettings settings)
        {
            Initialize(settings);
            ResetStateWithMapFor<T>();
        }

        public void DoSerialize(object obj)
        {
            Serializer.SerializeExactNonNullItem(obj, CurrentMapItem);
        }

        public T DoDeserialize<T>()
        {
            return (T)Deserializer.DeserializeExactNonNullItem(CurrentMapItem);
        }
    }
}
