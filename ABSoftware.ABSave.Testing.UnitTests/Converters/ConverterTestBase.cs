using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Mapping.Items;
using ABSoftware.ABSave.Serialization;
using ABSoftware.ABSave.Testing.UnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABSoftware.ABSave.Testing.UnitTests.Converters
{
    public class ConverterTestBase : TestBase
    {
        public ABSaveConverter CurrentConverter;

        public void Setup<T>(ABSaveSettings settings, ABSaveConverter converter)
        {
            Initialize(settings);
            ResetOutputWithMapItem(new ConverterMapItem(MapGenerator.GenerateItemType(typeof(T)), converter, converter.TryGenerateContext(CurrentMap, typeof(T))));
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
