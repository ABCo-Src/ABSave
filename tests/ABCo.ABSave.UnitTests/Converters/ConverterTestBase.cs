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

        public void Setup<T>(ABSaveSettings settings, Dictionary<Type, uint> targetVersions = null, bool includeVersioning = true) => Setup(settings, typeof(T), targetVersions, includeVersioning);
        public void Setup(ABSaveSettings settings, Type type, Dictionary<Type, uint> targetVersions = null, bool includeVersioning = true)
        {
            Initialize(settings, targetVersions, false, includeVersioning);
            ResetStateWithMapFor(type);
        }

        public void DoSerialize(object obj)
        {
            Serializer.WriteExactNonNullItem(obj, CurrentMapItem);
            Serializer.Flush();
        }

        public T DoDeserialize<T>() => (T)Deserializer.ReadExactNonNullItem(CurrentMapItem);
        public object DoDeserialize() => Deserializer.ReadExactNonNullItem(CurrentMapItem);
    }
}
