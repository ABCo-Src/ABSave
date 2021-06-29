using ABCo.ABSave.Deserialization;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Mapping.Generation;
using ABCo.ABSave.Serialization;
using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

namespace ABCo.ABSave.Converters
{
    [Select(typeof(string))]
    [Select(typeof(StringBuilder))]
    [Select(typeof(char[]))]
    public class TextConverter : Converter
    {
        StringType _type;

        public override bool CheckType(CheckTypeInfo info)
        {
            if (info.Type == typeof(string))
                _type = StringType.String;
            else if (info.Type == typeof(StringBuilder))
                _type = StringType.StringBuilder;
            else if (info.Type == typeof(char[]))
                _type = StringType.CharArray;
            else
                return false;

            return true;
        }

        #region Serialization

        public override void Serialize(in SerializeInfo info, ref BitTarget header)
        {
            switch (_type)
            {
                case StringType.String:
                    header.Serializer.WriteString((string)info.Instance, ref header);
                    break;
                case StringType.CharArray:
                    SerializeCharArray((char[])info.Instance, ref header);
                    break;
                case StringType.StringBuilder:

                    SerializeStringBuilder((StringBuilder)info.Instance, ref header);
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

        public override object Deserialize(in DeserializeInfo info, ref BitSource header) => _type switch
        {
            StringType.String => header.Deserializer.ReadString(ref header),
            StringType.CharArray => DeserializeCharArray(ref header),
            StringType.StringBuilder => DeserializeStringBuilder(ref header),
            _ => throw new Exception("Invalid StringType in text converter context"),
        };

        public static char[] DeserializeCharArray(ref BitSource header)
        {
            if (header.Deserializer.Settings.UseUTF8)
                return header.Deserializer.ReadUTF8(s => new char[s], c => c.AsMemory(), ref header);
            else
            {
                int size = (int)header.Deserializer.ReadCompressedInt(ref header);
                char[]? chArr = new char[size];

                header.Deserializer.FastReadShorts(MemoryMarshal.Cast<char, short>(chArr.AsSpan()));

                return chArr;
            }
        }

        public static StringBuilder DeserializeStringBuilder(ref BitSource header) => new StringBuilder(header.Deserializer.ReadString(ref header));

        #endregion

        #region Context
        enum StringType
        {
            String,
            StringBuilder,
            CharArray
        }

        #endregion

        public override (VersionInfo?, bool) GetVersionInfo(InitializeInfo info, uint version) => (null, true);
    }
}
