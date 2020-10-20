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

        public override bool HasNonExactTypes => false;
        public override Type[] ExactTypes { get; } = new Type[] { typeof(DictionaryEntry) };

        public override void Serialize(object obj, Type type, ABSaveWriter writer)
        {
            var pair = (DictionaryEntry)obj;

            ABSaveItemConverter.Serialize(pair.Key, typeof(object), writer);
            ABSaveItemConverter.Serialize(pair.Value, typeof(object), writer);
        }
        public override object Deserialize(Type type, ABSaveReader reader)
        {
            var key = ABSaveItemConverter.DeserializeWithAttribute(typeof(object), reader);
            var value = ABSaveItemConverter.DeserializeWithAttribute(typeof(object), reader);

            return new DictionaryEntry(key, value);
        }
    }
}
