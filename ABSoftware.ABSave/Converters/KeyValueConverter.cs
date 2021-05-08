using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Exceptions;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Mapping.Generation;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Converters
{
    public class KeyValueConverter : Converter
    {
        public static KeyValueConverter Instance { get; } = new KeyValueConverter();
        private KeyValueConverter() { }

        public override bool ConvertsSubTypes => true;
        public override bool AlsoConvertsNonExact => true;
        public override Type[] ExactTypes { get; } = new Type[]
        {
            typeof(DictionaryEntry)
        };

        public override void Serialize(object obj, Type actualType, IConverterContext context, ref BitTarget header)
        {
            var actualContext = (Context)context;

            if (actualContext.IsGeneric)
                SerializeGeneric((dynamic)obj, actualContext, header.Serializer);
                
            else
                SerializeNonGeneric((DictionaryEntry)obj, actualContext, header.Serializer);
        }

        static void SerializeGeneric(dynamic obj, Context context, ABSaveSerializer serializer)
        {
            serializer.SerializeItem(obj.Key, context.KeyMap);
            serializer.SerializeItem(obj.Value, context.ValueMap);
        }

        static void SerializeNonGeneric(DictionaryEntry obj, Context context, ABSaveSerializer serializer)
        {
            serializer.SerializeItem(obj.Key, context.KeyMap);
            serializer.SerializeItem(obj.Value, context.ValueMap);
        }

        public override object Deserialize(Type actualType, IConverterContext context, ref BitSource header)
        {
            var actualContext = (Context)context;

            if (actualContext.IsGeneric)
                return DeserializeGeneric(actualType, actualContext, header.Deserializer);
            else
                return DeserializeNonGeneric(header.Deserializer, actualContext);
        }

        static object DeserializeGeneric(Type actualType, Context context, ABSaveDeserializer deserializer)
        {
            var key = deserializer.DeserializeItem(context.KeyMap);
            var value = deserializer.DeserializeItem(context.ValueMap);

            return Activator.CreateInstance(actualType, key, value)!;
        }

        static DictionaryEntry DeserializeNonGeneric(ABSaveDeserializer deserializer, Context context)
        {
            var key = deserializer.DeserializeItem(context.KeyMap);
            var value = deserializer.DeserializeItem(context.ValueMap);

            if (key == null) throw new NullDictionaryKeyException();
            return new DictionaryEntry(key, value);
        }

        public override IConverterContext? TryGenerateContext(ref ContextGen gen)
        {
            var context = new Context();

            if (gen.Type.IsGenericType && gen.Type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                gen.MarkCanConvert();
                var genericArgs = gen.Type.GetGenericArguments();

                context.IsGeneric = true;
                context.KeyMap = gen.GetMap(genericArgs[0]);
                context.ValueMap = gen.GetMap(genericArgs[1]);
                return context;
            }
            else if (gen.Type == typeof(DictionaryEntry))
            {
                gen.MarkCanConvert();

                if (!gen.Settings.BypassDangerousTypeChecking) throw new DangerousTypeException("a general 'DictionaryEntry' type that could contain any type of element.");
                context.IsGeneric = false;
                context.KeyMap = context.ValueMap = gen.GetMap(typeof(object));
                return context;
            }
            else return null;
        }

        class Context : IConverterContext
        {
            public bool IsGeneric;
            public MapItemInfo KeyMap;
            public MapItemInfo ValueMap;
        }
    }
}
