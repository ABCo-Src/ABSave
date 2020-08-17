using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Converters
{
    public class KeyValueConverter : ABSaveTypeConverter
    {
        public static readonly KeyValueConverter Instance = new KeyValueConverter();

        private KeyValueConverter() { }

        public override bool HasExactType => false;
        public override bool CheckCanConvertType(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);

        public override void Serialize(object obj, Type type, ABSaveWriter writer)
        {
            var keySpecifiedType = type.GetGenericArguments()[0];
            var valueSpecifiedType = type.GetGenericArguments()[1];

            dynamic pair = (dynamic)obj;

            var keyVal = pair.Key;
            ABSaveItemConverter.SerializeWithAttribute(keyVal, keySpecifiedType, writer);

            var valueVal = pair.Value;
            ABSaveItemConverter.SerializeWithAttribute(valueVal, valueSpecifiedType, writer);
        }

        public override object Deserialize(Type type, ABSaveReader reader)
        {
            var keySpecifiedType = type.GetGenericArguments()[0];
            var valueSpecifiedType = type.GetGenericArguments()[1];

            var keyValuePair = (dynamic)Activator.CreateInstance(type);

            keyValuePair.Key = ABSaveItemConverter.DeserializeWithAttribute(keySpecifiedType, reader);
            keyValuePair.Value = ABSaveItemConverter.DeserializeWithAttribute(valueSpecifiedType, reader);
            return keyValuePair;
        }
    }
}
