using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Serialization;
using ABSoftware.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABSoftware.ABSave.UnitTests.Converters
{
    public class ConverterTestBase : TestBase
    {
        public Converter CurrentConverter = null!;

        public void Setup<T>(ABSaveSettings settings, Converter converter)
        {
            Initialize(settings);
            ResetStateWithConverter<T>(converter);
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
