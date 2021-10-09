using ABCo.ABSave.Configuration;
using ABCo.ABSave.Serialization.Converters;
using ABCo.ABSave.UnitTests.TestHelpers;
using System;
using System.Collections.Generic;

namespace ABCo.ABSave.UnitTests.Converters
{
    public class ConverterTestBase : TestBase
    {
        public Converter CurrentConverter = null!;

        public void Setup<T>(ABSaveSettings settings, Dictionary<Type, uint> targetVersions = null)
        {
            Initialize(settings, targetVersions);
            ResetStateWithMapFor<T>();
        }

        public void DoSerialize(object obj)
        {
            Serializer.WriteExactNonNullItem(obj, CurrentMapItem);
            Serializer.Flush();
        }

        public T DoDeserialize<T>()
        {
            return (T)Deserializer.ReadExactNonNullItem(CurrentMapItem);
        }
    }
}
