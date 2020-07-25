using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Converters.Internal
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
