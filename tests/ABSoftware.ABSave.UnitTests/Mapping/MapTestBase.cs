using ABCo.ABSave.Converters;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Generation;
using ABCo.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABCo.ABSave.UnitTests.Mapping
{
    public abstract class MapTestBase
    {
        public ABSaveMap Map;
        public MapGenerator Generator;

        public void Setup()
        {
            var builder = new ABSaveSettingsBuilder();
            builder.CustomConverters = new List<Converter>() { new SubTypeConverter() };
            var settings = builder.CreateSettings(ABSaveSettings.ForSpeed);

            Map = new ABSaveMap(settings);
            Generator = new MapGenerator();
            Generator.Initialize(Map);
        }
    }
}
