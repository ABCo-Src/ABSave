using ABCo.ABSave.Serialization.Reading;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Mapping.Generation.Converters;
using ABCo.ABSave.Serialization.Writing;
using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

namespace ABCo.ABSave.Serialization.Converters
{
    [Select(typeof(string))]
    [Select(typeof(StringBuilder))]
    [Select(typeof(char[]))]
    public class TextConverter : Converter
    {
        StringType _type;

        public override uint Initialize(InitializeInfo info)
        {
            if (info.Type == typeof(string))
                _type = StringType.String;
            else if (info.Type == typeof(StringBuilder))
                _type = StringType.StringBuilder;
            else if (info.Type == typeof(char[]))
                _type = StringType.CharArray;

            return 0;
        }

        #region Serialization

        public override void Serialize(in SerializeInfo info)
        {
            switch (_type)
            {
                case StringType.String:
                    info.Serializer.WriteNonNullString((string)info.Instance);
                    break;
                case StringType.CharArray:
                    SerializeCharArray((char[])info.Instance, info.Serializer);
                    break;
                case StringType.StringBuilder:

                    SerializeStringBuilder((StringBuilder)info.Instance, info.Serializer);
                    break;
            }
        }

        public static void SerializeCharArray(char[] obj, ABSaveSerializer header) =>
            header.WriteText(obj.AsSpan());

        public static void SerializeStringBuilder(StringBuilder obj, ABSaveSerializer header)
        {
            // TODO: Use "GetChunks" with .NET 5!
            char[] tmp = obj.Length < ABSaveUtils.MAX_STACK_SIZE ? new char[obj.Length] : ArrayPool<char>.Shared.Rent(obj.Length);
            obj.CopyTo(0, tmp, 0, obj.Length);

            header.WriteText(new ReadOnlySpan<char>(tmp));
            if (obj.Length >= ABSaveUtils.MAX_STACK_SIZE) ArrayPool<char>.Shared.Return(tmp);
        }

        #endregion

        #region Deserialization

        public override object Deserialize(in DeserializeInfo info) => _type switch
        {
            StringType.String => info.Deserializer.ReadNonNullString(),
            StringType.CharArray => DeserializeCharArray(info.Deserializer),
            StringType.StringBuilder => DeserializeStringBuilder(info.Deserializer),
            _ => throw new Exception("Invalid StringType in text converter context"),
        };

        public static char[] DeserializeCharArray(ABSaveDeserializer deserializer)
        {
            if (deserializer.State.Settings.UseUTF8)
                return deserializer.ReadUTF8(s => new char[s], c => c.AsMemory());
            else
            {
                int size = (int)deserializer.ReadCompressedInt();
                char[]? chArr = new char[size];

                deserializer.FastReadShorts(MemoryMarshal.Cast<char, short>(chArr.AsSpan()));

                return chArr;
            }
        }

        public static StringBuilder DeserializeStringBuilder(ABSaveDeserializer deserializer) => new StringBuilder(deserializer.ReadNonNullString());

        #endregion

        #region Context
        enum StringType
        {
            String,
            StringBuilder,
            CharArray
        }

        #endregion
    }
}
