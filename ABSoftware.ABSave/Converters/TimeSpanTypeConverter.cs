using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization;
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

        public override void Serialize(object obj, TypeInformation typeInfo, ABSaveWriter writer) => writer.WriteInt64((ulong)((TimeSpan)obj).Ticks);
    }
}
