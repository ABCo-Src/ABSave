using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization.Writer;
using System;
using System.Text;

namespace ABSoftware.ABSave.Converters
{
    public class StringBuilderTypeConverter : ABSaveTypeConverter
    {
        public static StringBuilderTypeConverter Instance = new StringBuilderTypeConverter();
        private StringBuilderTypeConverter() { }

        public override bool HasExactType => true;
        public override Type ExactType => typeof(StringBuilder);

        public override void Serialize(object obj, TypeInformation typeInfo, ABSaveWriter writer)
        {
            writer.WriteText((StringBuilder)obj);
        }
    }
}
