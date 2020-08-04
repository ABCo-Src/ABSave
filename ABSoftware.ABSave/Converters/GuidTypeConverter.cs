using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization;
using System;

namespace ABSoftware.ABSave.Converters
{
    public class GuidTypeConverter : ABSaveTypeConverter
    {
        public static GuidTypeConverter Instance = new GuidTypeConverter();
        private GuidTypeConverter() { }
        public override bool HasExactType => true;

        public override void Serialize(object obj, TypeInformation typeInfo, ABSaveWriter writer)
        {
            writer.WriteByteArray(((Guid)obj).ToByteArray(), false);
        }
    }
}
