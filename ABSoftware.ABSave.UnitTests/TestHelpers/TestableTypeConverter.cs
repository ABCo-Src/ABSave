using ABSoftware.ABSave.Converters;
using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Mapping.Generation;
using ABSoftware.ABSave.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABSoftware.ABSave.UnitTests.TestHelpers
{
    // A type converter with customizable properties for easy testing.
    class  TestableTypeConverter : Converter
    {
        public const int OUTPUT_BYTE = 55;

        private bool _writesToHeader;
        
        public override bool UsesHeaderForVersion(uint version) => _writesToHeader;        

        public TestableTypeConverter(bool writesToHeader) => _writesToHeader = writesToHeader;

        public MapItem GetMap<T>() => GetMap(typeof(T));
        public MapItem GetMap(Type itemType)
        {
            throw new Exception("TODO");
            //return new Mapping.Items.ConverterMapItem(new MapGenerator().GetMap(itemType), this, new Context());
        }

        public override bool AlsoConvertsNonExact => true;
        public override Type[] ExactTypes => new Type[] { typeof(Base), typeof(int) };
        public override void TryGenerateContext(ref ContextGen gen)
        {
            if (gen.Type == typeof(Base) || gen.Type.IsSubclassOf(typeof(Base)) || gen.Type == typeof(int))
                gen.AssignContext(new Context(), 0);
        }

        class Context : ConverterContext { }

        public override void Serialize(object obj, Type actualType, ConverterContext context, ref BitTarget header)
        {
            if (_writesToHeader)
            {
                header.WriteBitOn();
                header.Apply();
            }

            header.Serializer.WriteByte(OUTPUT_BYTE);
        }

        public override object Deserialize(Type actualType, ConverterContext context, ref BitSource header)
        {
            if (_writesToHeader && !header.ReadBit()) throw new Exception("Deserialize read invalid header bit");
            if (header.Deserializer.ReadByte() != OUTPUT_BYTE) throw new Exception("Deserialize read invalid byte");

            return OUTPUT_BYTE;
        }
    }
}