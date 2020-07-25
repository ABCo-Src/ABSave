using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization.Writer;
using System;

namespace ABSoftware.ABSave.Converters.Internal
{
    public class DateTimeTypeConverter : ABSaveTypeConverter
    {
        public static DateTimeTypeConverter Instance = new DateTimeTypeConverter();
        private DateTimeTypeConverter() { }

        public override bool HasExactType => true;
        public override Type ExactType => typeof(DateTime);

        public override void Serialize(object obj, TypeInformation typeInfo, ABSaveWriter writer) => writer.WriteInt64((ulong)((DateTime)obj).Ticks);
    }
}
