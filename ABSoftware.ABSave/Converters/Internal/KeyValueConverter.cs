using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization;
using ABSoftware.ABSave.Serialization.Writer;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Converters.Internal
{
    public class KeyValueConverter : ABSaveTypeConverter
    {
        public static readonly KeyValueConverter Instance = new KeyValueConverter();

        private KeyValueConverter() { }

        public override bool HasExactType => false;
        public override bool CheckCanConvertType(TypeInformation typeInformation) => typeInformation.ActualType.IsGenericType && typeInformation.ActualType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);

        public override void Serialize(object obj, TypeInformation typeInfo, ABSaveWriter writer)
        {
            var keySpecifiedType = typeInfo.ActualType.GetGenericArguments()[0];
            var valueSpecifiedType = typeInfo.ActualType.GetGenericArguments()[1];

            dynamic pair = (dynamic)obj;

            var keyVal = pair.Key;
            var keyActualType = keyVal.GetType();
            ABSaveItemSerializer.SerializeAuto(keyVal, writer, new TypeInformation(keyActualType, Type.GetTypeCode(keyActualType)));

            var valueVal = pair.Value;
            var valueActualType = valueVal.GetType();
            ABSaveItemSerializer.SerializeAuto(pair.Value, writer, new TypeInformation(valueActualType, Type.GetTypeCode(valueActualType)));
        }
    }
}
