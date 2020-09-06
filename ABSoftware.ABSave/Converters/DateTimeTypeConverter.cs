using System;

namespace ABSoftware.ABSave.Converters
{
    public class DateTimeTypeConverter : ABSaveTypeConverter
    {
        public static DateTimeTypeConverter Instance = new DateTimeTypeConverter();
        private DateTimeTypeConverter() { }

        public override bool HasExactType => true;
        public override Type ExactType => typeof(DateTime);

        public override void Serialize(object obj, Type type, ABSaveWriter writer) => writer.WriteInt64((ulong)((DateTime)obj).Ticks);
        public override object Deserialize(Type type, ABSaveReader reader) => new DateTime((long)reader.ReadInt64());
    }
}
