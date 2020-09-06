using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Converters
{
    public class TimeSpanTypeConverter : ABSaveTypeConverter
    {
        public readonly static TimeSpanTypeConverter Instance = new TimeSpanTypeConverter();
        private TimeSpanTypeConverter() { }

        public override bool HasExactType => true;
        public override Type ExactType => typeof(TimeSpan);

        public override void Serialize(object obj, Type type, ABSaveWriter writer) => writer.WriteInt64((ulong)((TimeSpan)obj).Ticks);
        public override object Deserialize(Type type, ABSaveReader reader) => new TimeSpan((long)reader.ReadInt64());
    }
}
