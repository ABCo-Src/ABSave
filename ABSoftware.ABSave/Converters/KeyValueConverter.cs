using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Converters
{
    public class KeyValueConverter : ABSaveConverter
    {
        public static KeyValueConverter Instance { get; } = new KeyValueConverter();
        private KeyValueConverter() { }

        public override bool ConvertsSubTypes => true;
        public override bool AlsoConvertsNonExact => true;
        public override Type[] ExactTypes { get; } = new Type[]
        {
            typeof(DictionaryEntry)
        };

        public override void Serialize(object obj, Type actualType, IABSaveConverterContext context, ref BitTarget header)
        {
            var actualContext = (Context)context;

            if (actualContext.IsGeneric)
                SerializeGeneric((dynamic)obj, actualContext, header.Serializer);
                
            else
                SerializeNonGeneric((DictionaryEntry)obj, header.Serializer);
        }

        void SerializeGeneric(dynamic obj, Context context, ABSaveSerializer serializer)
        {
            serializer.SerializeItem(obj.Key, context.KeyMap);
            serializer.SerializeItem(obj.Value, context.ValueMap);
        }

        void SerializeNonGeneric(DictionaryEntry obj, ABSaveSerializer serializer)
        {
            var keyMap = serializer.GetRuntimeMapItem(obj.Key.GetType());
            var valueMap = serializer.GetRuntimeMapItem(obj.Value.GetType());

            serializer.WriteClosedType(keyMap.ItemType);
            serializer.WriteClosedType(valueMap.ItemType);

            serializer.SerializeExactNonNullItem(obj.Key, keyMap);
            serializer.SerializeExactNonNullItem(obj.Value, valueMap);
        }

        public override object Deserialize(Type actualType, IABSaveConverterContext context, ref BitSource header)
        {
            var actualContext = (Context)context;

            if (actualContext.IsGeneric)
                return DeserializeGeneric(actualType, actualContext, header.Deserializer);
            else
                return DeserializeNonGeneric(header.Deserializer);
        }

        object DeserializeGeneric(Type actualType, Context context, ABSaveDeserializer deserializer)
        {
            var key = deserializer.DeserializeItem(context.KeyMap);
            var value = deserializer.DeserializeItem(context.ValueMap);

            return Activator.CreateInstance(actualType, key, value);
        }

        DictionaryEntry DeserializeNonGeneric(ABSaveDeserializer deserializer)
        {
            var keyMap = deserializer.GetRuntimeMapItem(deserializer.ReadClosedType());
            var valueMap = deserializer.GetRuntimeMapItem(deserializer.ReadClosedType());

            var key = deserializer.DeserializeExactNonNullItem(keyMap);
            var value = deserializer.DeserializeExactNonNullItem(valueMap);

            return new DictionaryEntry(key, value);
        }

        public override IABSaveConverterContext TryGenerateContext(ABSaveMap map, Type type)
        {
            var context = new Context();

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                var genericArgs = type.GetGenericArguments();

                context.IsGeneric = true;
                context.KeyMap = map.GetMaptimeSubItem(genericArgs[0]);
                context.ValueMap = map.GetMaptimeSubItem(genericArgs[1]);
                return context;
            }
            else if (type == typeof(DictionaryEntry))
            {
                context.IsGeneric = false;
                context.KeyMap = context.ValueMap = map.GetMaptimeSubItem(typeof(object));
                return context;
            }
            else return null;
        }

        class Context : IABSaveConverterContext
        {
            public bool IsGeneric;
            public MapItem KeyMap;
            public MapItem ValueMap;
        }
    }
}
