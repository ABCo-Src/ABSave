using ABCo.ABSave.Deserialization;
using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Mapping.Generation;
using ABCo.ABSave.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ABCo.ABSave.Converters
{
    [Select(typeof(DictionaryEntry), typeof(object))]
    [Select(typeof(KeyValuePair<,>), 0, 1)]
    public class KeyValueConverter : Converter
    {
        bool _isGeneric;
        MapItemInfo _keyMap;
        MapItemInfo _valueMap;

        public override void Serialize(in SerializeInfo info, ref BitTarget header)
        {
            if (_isGeneric)
            {
                SerializeGeneric((dynamic)info.Instance, header.Serializer);
            }
            else
            {
                SerializeNonGeneric((DictionaryEntry)info.Instance, header.Serializer);
            }
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

        public override object Deserialize(in DeserializeInfo info, ref BitSource header)
        {
            if (_isGeneric)
            {
                return DeserializeGeneric(info.ActualType, header.Deserializer);
            }
            else
            {
                return DeserializeNonGeneric(header.Deserializer);
            }
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

            if (key == null)
            {
                throw new NullDictionaryKeyException();
            }

            return new DictionaryEntry(key, value);
        }

        public override void Initialize(InitializeInfo info)
        {
            _isGeneric = info.Type.IsGenericType;
            // KeyValuePair<,>
            if (_isGeneric)
            {
                var genericArgs = info.Type.GetGenericArguments();

                _keyMap = info.GetMap(genericArgs[0]);
                _valueMap = info.GetMap(genericArgs[1]);
            }

            // DictionaryEntry
            else
            {
                _keyMap = _valueMap = info.GetMap(typeof(object));
            }
        }
    }
}
