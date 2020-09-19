using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ABSoftware.ABSave.Converters
{
    public class DictionaryEntryConverter : ABSaveTypeConverter
    {
        public readonly static DictionaryEntryConverter Instance = new DictionaryEntryConverter();
        private DictionaryEntryConverter() { }

        public override bool HasExactType => true;
        public override Type ExactType => typeof(DictionaryEntry);

        public override void Serialize(object obj, Type type, ABSaveWriter writer)
        {
            var pair = (DictionaryEntry)obj;

            ABSaveItemConverter.SerializeWithAttribute(pair.Key, typeof(object), writer);
            ABSaveItemConverter.SerializeWithAttribute(pair.Value, typeof(object), writer);
        }
        public override object Deserialize(Type type, ABSaveReader reader)
        {
            var key = ABSaveItemConverter.DeserializeWithAttribute(typeof(object), reader);
            var value = ABSaveItemConverter.DeserializeWithAttribute(typeof(object), reader);

            return new DictionaryEntry(key, value);
        }
    }
}
