using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.UnitTests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABSoftware.ABSave.UnitTests.Mapping
{
    public abstract class MapTestBase
    {
        public ABSaveMap Map;
        public MapGenerator Generator;

        public void Setup()
        {
            var settings = ABSaveSettings.ForSize;

            Map = new ABSaveMap(settings);
            Generator = new MapGenerator();
            Generator.Initialize(Map);
        }
    }
}
