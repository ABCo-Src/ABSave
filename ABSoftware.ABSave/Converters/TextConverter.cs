using ABCo.ABSave.Deserialization;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Generation;
using ABCo.ABSave.Serialization;
using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

namespace ABCo.ABSave.Converters
{
    public class TextConverter : Converter
    {
        public static TextConverter Instance { get; } = new TextConverter();
        private TextConverter() { }

        public override bool AlsoConvertsNonExact => false;        
        public override bool UsesHeaderForVersion(uint version) => true;
        public override Type[] ExactTypes { get; } = new Type[] { typeof(string), typeof(StringBuilder), typeof(char[]) };

        #region Serialization

        public override void Serialize(object obj, Type actualType, ConverterContext context, ref BitTarget header)
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

        public static void SerializeCharArray(char[] obj, ref BitTarget header) =>
            header.Serializer.WriteText(obj.AsSpan(), ref header);

        public static void SerializeStringBuilder(StringBuilder obj, ref BitTarget header)
        {
            // TODO: Use "GetChunks" with .NET 5!
            char[] tmp = obj.Length < ABSaveUtils.MAX_STACK_SIZE ? new char[obj.Length] : ArrayPool<char>.Shared.Rent(obj.Length);
            obj.CopyTo(0, tmp, 0, obj.Length);

            header.Serializer.WriteText(new ReadOnlySpan<char>(tmp), ref header);
            ArrayPool<char>.Shared.Return(tmp);
        }

        #endregion

        #region Deserialization

        public override object Deserialize(Type actualType, ConverterContext context, ref BitSource header)
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

        public static char[] DeserializeCharArray(ref BitSource header)
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

        public static StringBuilder DeserializeStringBuilder(ref BitSource header) => new StringBuilder(header.Deserializer.ReadString(ref header));

        #endregion

        #region Context
        public override void TryGenerateContext(ref ContextGen gen)
        {
            if (gen.Type == typeof(string))
                gen.AssignContext(Context.String, 0);
            else if (gen.Type == typeof(StringBuilder))
                gen.AssignContext(Context.StringBuilder, 0);
            else if (gen.Type == typeof(char[])) 
                gen.AssignContext(Context.CharArray, 0);
        }

        enum StringType
        {
            String,
            StringBuilder,
            CharArray
        }

        class Context : ConverterContext
        {
            public static readonly Context String = new Context() { Type = StringType.String };
            public static readonly Context StringBuilder = new Context() { Type = StringType.StringBuilder };
            public static readonly Context CharArray = new Context() { Type = StringType.StringBuilder };

            public StringType Type;
        }
        #endregion
    }
}
