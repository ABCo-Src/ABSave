using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Mapping.Items;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABSoftware.ABSave.Testing.UnitTests.Helpers
{
    // A type converter with customizable properties for easy testing.
    class TestableTypeConverter : ABSaveConverter
    {
        public const int OUTPUT_BYTE = 55;

        private bool _writesToHeader;
        private bool _convertsSubTypes;

        public override bool WritesToHeader => _writesToHeader;
        public override bool ConvertsSubTypes => _convertsSubTypes;

        public TestableTypeConverter(bool writesToHeader, bool convertsSubTypes) => (_writesToHeader, _convertsSubTypes) = (writesToHeader, convertsSubTypes);

        public MapItem GetMap<T>() => GetMap(typeof(T));
        public MapItem GetMap(Type itemType)
        {
            return new ConverterMapItem(itemType, itemType.IsValueType, this, new Context());
        }

        public override bool AlsoConvertsNonExact => true;
        public override Type[] ExactTypes => new Type[] { typeof(Base), typeof(int) };
        public override IABSaveConverterContext TryGenerateContext(ABSaveMap map, Type type)
        {
            if (type.IsSubclassOf(typeof(Base)) || type == typeof(int))
            {
                return new Context();
            }

            return null;
        }

        class Context : IABSaveConverterContext { }

        public override void Serialize(object obj, Type actualType, IABSaveConverterContext context, ref BitTarget header)
        {
            if (_writesToHeader)
            {
                header.WriteBitOn();
                header.Apply();
            }

            header.Serializer.WriteByte(OUTPUT_BYTE);
        }

        public override object Deserialize(Type actualType, IABSaveConverterContext context, ref BitSource header)
        {
            if (_writesToHeader && !header.ReadBit()) throw new Exception("Deserialize read invalid header bit");
            if (header.Deserializer.ReadByte() != OUTPUT_BYTE) throw new Exception("Deserialize read invalid byte");

            return OUTPUT_BYTE;
        }
    }
}