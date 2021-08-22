using ABCo.ABSave.Serialization.Reading;
using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Mapping.Generation.Converters;
using ABCo.ABSave.Serialization.Writing;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ABCo.ABSave.Serialization.Converters
{
    [Select(typeof(DictionaryEntry), typeof(object))]
    [Select(typeof(KeyValuePair<,>), 0, 1)]
    public class KeyValueConverter : Converter
    {
        bool _isGeneric;
        MapItemInfo _keyMap;
        MapItemInfo _valueMap;

        public override void Serialize(in SerializeInfo info)
        {
            if (_isGeneric)
                SerializeGeneric((dynamic)info.Instance, info.Serializer);

            else
                SerializeNonGeneric((DictionaryEntry)info.Instance, info.Serializer);
        }

        void SerializeGeneric(dynamic obj, ABSaveSerializer serializer)
        {
            serializer.WriteItem(obj.Key, _keyMap);
            serializer.WriteItem(obj.Value, _valueMap);
        }

        void SerializeNonGeneric(DictionaryEntry obj, ABSaveSerializer serializer)
        {
            serializer.WriteItem(obj.Key, _keyMap);
            serializer.WriteItem(obj.Value, _valueMap);
        }

        public override object Deserialize(in DeserializeInfo info)
        {
            if (_isGeneric)
                return DeserializeGeneric(info.ActualType, info.Deserializer);
            else
                return DeserializeNonGeneric(info.Deserializer);
        }

        object DeserializeGeneric(Type actualType, ABSaveDeserializer deserializer)
        {
            object? key = deserializer.ReadItem(_keyMap);
            object? value = deserializer.ReadItem(_valueMap);

            return Activator.CreateInstance(actualType, key, value)!;
        }

        DictionaryEntry DeserializeNonGeneric(ABSaveDeserializer deserializer)
        {
            object? key = deserializer.ReadItem(_keyMap);
            object? value = deserializer.ReadItem(_valueMap);

            if (key == null) throw new NullDictionaryKeyException();
            return new DictionaryEntry(key, value);
        }

        public override uint Initialize(InitializeInfo info)
        {
            _isGeneric = info.Type.IsGenericType;
            // KeyValuePair<,>
            if (_isGeneric)
            {
                Type[]? genericArgs = info.Type.GetGenericArguments();

                _keyMap = info.GetMap(genericArgs[0]);
                _valueMap = info.GetMap(genericArgs[1]);
            }

            // DictionaryEntry
            else
            {
                _keyMap = _valueMap = info.GetMap(typeof(object));
            }

            return 0;
        }
    }
}
