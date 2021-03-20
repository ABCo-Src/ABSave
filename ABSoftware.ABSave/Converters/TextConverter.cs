using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Mapping.Generation;
using ABSoftware.ABSave.Serialization;
using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

namespace ABSoftware.ABSave.Converters
{
    public class TextConverter : Converter
    {
        public static TextConverter Instance { get; } = new TextConverter();
        private TextConverter() { }

        public override bool AlsoConvertsNonExact => false;
        public override bool WritesToHeader => true;
        public override bool ConvertsSubTypes => false;
        public override Type[] ExactTypes { get; } = new Type[] { typeof(string), typeof(StringBuilder), typeof(char[]) };

        #region Serialization

        public override void Serialize(object obj, Type actualType, IConverterContext context, ref BitTarget header)
        {
            var info = (Context)context;

            switch (info.Type)
            {
                case StringType.String:
                    header.Serializer.WriteString((string)obj, ref header);
                    break;
                case StringType.CharArray:
                    SerializeCharArray((char[])obj, ref header);
                    break;
                case StringType.StringBuilder:
                    SerializeStringBuilder((StringBuilder)obj, ref header);
                    break;
            }
        }

        public unsafe void SerializeCharArray(char[] obj, ref BitTarget header)
        {
             SerializeCharacters(obj.AsSpan(), ref header);
        }

        public void SerializeStringBuilder(StringBuilder obj, ref BitTarget header)
        {
            // TODO: Use "GetChunks" with .NET 5!
            char[] tmp = obj.Length < ABSaveUtils.MAX_STACK_SIZE ? new char[obj.Length] : ArrayPool<char>.Shared.Rent(obj.Length);
            obj.CopyTo(0, tmp, 0, obj.Length);

            SerializeCharacters(new ReadOnlySpan<char>(tmp), ref header);
            ArrayPool<char>.Shared.Return(tmp);
        }

        public unsafe void SerializeCharacters(ReadOnlySpan<char> txt, ref BitTarget header)
        {
            if (header.Serializer.Settings.UseUTF8)
                header.Serializer.WriteUTF8(txt, ref header);
            else
            {
                header.Serializer.WriteCompressed((uint)txt.Length, ref header);
                header.Serializer.FastWriteShorts(MemoryMarshal.Cast<char, short>(txt));
            }
        }

        #endregion

        #region Deserialization

        public override object Deserialize(Type actualType, IConverterContext context, ref BitSource header)
        {
            var info = (Context)context;

            return info.Type switch
            {
                StringType.String => header.Deserializer.ReadString(ref header),
                StringType.CharArray => DeserializeCharArray(ref header),
                StringType.StringBuilder => DeserializeStringBuilder(ref header),
                _ => throw new Exception("Invalid StringType in text converter context"),
            };
        }

        public unsafe char[] DeserializeCharArray(ref BitSource header)
        {
            if (header.Deserializer.Settings.UseUTF8)
                return header.Deserializer.ReadUTF8(s => new char[s], c => c.AsMemory(), ref header);
            else
            {
                int size = (int)header.Deserializer.ReadCompressedInt(ref header);
                var chArr = new char[size];

                header.Deserializer.FastReadShorts(MemoryMarshal.Cast<char, short>(chArr.AsSpan()));

                return chArr;
            }
        }

        public StringBuilder DeserializeStringBuilder(ref BitSource header) => new StringBuilder(header.Deserializer.ReadString(ref header));

        #endregion

        #region Context
        public override IConverterContext TryGenerateContext(ref ContextGen gen)
        {
            if (gen.Type == typeof(string))
            {
                gen.MarkCanConvert();
                return Context.String;
            }
            else if (gen.Type == typeof(StringBuilder))
            {
                gen.MarkCanConvert();
                return Context.StringBuilder;
            }
            else if (gen.Type == typeof(char[])) 
            {
                gen.MarkCanConvert();
                return Context.CharArray;
            }

            else return null;
        }

        enum StringType
        {
            String,
            StringBuilder,
            CharArray
        }

        class Context : IConverterContext
        {
            public static readonly Context String = new Context() { Type = StringType.String };
            public static readonly Context StringBuilder = new Context() { Type = StringType.StringBuilder };
            public static readonly Context CharArray = new Context() { Type = StringType.StringBuilder };

            public StringType Type;
        }
        #endregion
    }
}
