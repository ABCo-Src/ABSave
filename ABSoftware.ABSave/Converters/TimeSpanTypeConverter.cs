using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Converters
{
    public class TimeSpanTypeConverter : ABSaveTypeConverter
    {
        public readonly static TimeSpanTypeConverter Instance = new TimeSpanTypeConverter();
        private TimeSpanTypeConverter() { }

        public override bool HasNonExactTypes => false;
        public override Type[] ExactTypes { get; } = new Type[] { typeof(TimeSpan) };

        public override void Serialize(object obj, Type type, ABSaveWriter writer) => writer.WriteInt64((ulong)((TimeSpan)obj).Ticks);
        public override object Deserialize(Type type, ABSaveReader reader) => new TimeSpan((long)reader.ReadInt64());
    }
}
