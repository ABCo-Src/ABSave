using ABCo.ABSave.Deserialization;
using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Generation;
using ABCo.ABSave.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Converters
{
    public class KeyValueConverter : Converter
    {
        bool _isGeneric;
        MapItemInfo _keyMap;
        MapItemInfo _valueMap;

        public override void Serialize(object obj, Type actualType, ref BitTarget header)
        {
            if (_isGeneric)
                SerializeGeneric((dynamic)obj, header.Serializer);
                
            else
                SerializeNonGeneric((DictionaryEntry)obj, header.Serializer);
        }

        void SerializeGeneric(dynamic obj, ABSaveSerializer serializer)
        {
            serializer.SerializeItem(obj.Key, _keyMap);
            serializer.SerializeItem(obj.Value, _valueMap);
        }

        void SerializeNonGeneric(DictionaryEntry obj, ABSaveSerializer serializer)
        {
            serializer.SerializeItem(obj.Key, _keyMap);
            serializer.SerializeItem(obj.Value, _valueMap);
        }

        public override object Deserialize(Type actualType, ref BitSource header)
        {
            if (_isGeneric)
                return DeserializeGeneric(actualType, header.Deserializer);
            else
                return DeserializeNonGeneric(header.Deserializer);
        }

        object DeserializeGeneric(Type actualType, ABSaveDeserializer deserializer)
        {
            var key = deserializer.DeserializeItem(_keyMap);
            var value = deserializer.DeserializeItem(_valueMap);

            return Activator.CreateInstance(actualType, key, value)!;
        }

        DictionaryEntry DeserializeNonGeneric(ABSaveDeserializer deserializer)
        {
            var key = deserializer.DeserializeItem(_keyMap);
            var value = deserializer.DeserializeItem(_valueMap);

            if (key == null) throw new NullDictionaryKeyException();
            return new DictionaryEntry(key, value);
        }

        public override void Initialize(InitializeInfo info)
        {
            if (_isGeneric)
            {
                var genericArgs = info.Type.GetGenericArguments();

                _keyMap = info.GetMap(genericArgs[0]);
                _valueMap = info.GetMap(genericArgs[1]);
            }
            else
            {
                _keyMap = _valueMap = info.GetMap(typeof(object));
            }
        }

        public override bool CheckType(CheckTypeInfo info) =>
            _isGeneric = info.Type.IsGenericType && info.Type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);

        public override bool AlsoConvertsNonExact => true;
        public override Type[] ExactTypes { get; } = new Type[]
        {
            typeof(DictionaryEntry)
        };
    }
}
