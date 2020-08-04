using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization;
using System;

namespace ABSoftware.ABSave.Converters
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
