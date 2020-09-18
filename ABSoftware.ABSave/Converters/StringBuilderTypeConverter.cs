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

        public override void Serialize(object obj, Type type, ABSaveWriter writer) => writer.WriteStringBuilder((StringBuilder)obj);
        public override object Deserialize(Type type, ABSaveReader reader) => reader.ReadStringBuilder();
    }
}
