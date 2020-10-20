using System;

namespace ABSoftware.ABSave.Converters
{
    public class StringTypeConverter : ABSaveTypeConverter
    {
        public static StringTypeConverter Instance = new StringTypeConverter();
        private StringTypeConverter() { }

        public override bool HasNonExactTypes => false;
        public override Type[] ExactTypes { get; } = new Type[] { typeof(string) };

        public override void Serialize(object obj, Type type, ABSaveWriter writer) => writer.WriteString((string)obj);
        public override object Deserialize(Type type, ABSaveReader reader) => reader.ReadString();
    }
}
