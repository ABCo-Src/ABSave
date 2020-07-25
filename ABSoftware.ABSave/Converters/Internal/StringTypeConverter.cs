using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Converters.Internal
{
    public class StringTypeConverter : ABSaveTypeConverter
    {
        public static StringTypeConverter Instance = new StringTypeConverter();
        private StringTypeConverter() { }

        public override bool HasExactType => true;
        public override Type ExactType => typeof(string);

        public override void Serialize(object obj, TypeInformation typeInfo, ABSaveWriter writer) => writer.WriteText((string)obj);
    }
}
