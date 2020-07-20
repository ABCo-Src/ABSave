using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Converters.Internal
{
    internal class TypeTypeConverter : ABSaveTypeConverter
    {
        public override bool ConvertsType(TypeInformation typeInformation)
        {
            throw new NotImplementedException();
        }

        public override bool Serialize(dynamic obj, ABSaveWriter writer, TypeInformation typeInformation)
        {
            throw new NotImplementedException();
        }
    }
}
