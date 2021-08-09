using ABCo.ABSave.Deserialization;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Mapping.Generation.Converters;
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

        public override void Serialize(in SerializeInfo info)
        {
            switch (_type)
            {
                case StringType.String:
                    info.Header.WriteNonNullString((string)info.Instance);
                    break;
                case StringType.CharArray:
                    SerializeCharArray((char[])info.Instance, info.Header);
                    break;
                case StringType.StringBuilder:

                    SerializeStringBuilder((StringBuilder)info.Instance, info.Header);
                    break;
            }
        }

        public static void SerializeCharArray(char[] obj, BitWriter header) =>
            header.WriteText(obj.AsSpan());

        public static void SerializeStringBuilder(StringBuilder obj, BitWriter header)
        {
            // TODO: Use "GetChunks" with .NET 5!
            char[] tmp = obj.Length < ABSaveUtils.MAX_STACK_SIZE ? new char[obj.Length] : ArrayPool<char>.Shared.Rent(obj.Length);
            obj.CopyTo(0, tmp, 0, obj.Length);

            header.WriteText(new ReadOnlySpan<char>(tmp));
            ArrayPool<char>.Shared.Return(tmp);
        }

        #endregion

        #region Deserialization

        public override object Deserialize(in DeserializeInfo info) => _type switch
        {
            StringType.String => info.Header.ReadString(),
            StringType.CharArray => DeserializeCharArray(info.Header),
            StringType.StringBuilder => DeserializeStringBuilder(info.Header),
            _ => throw new Exception("Invalid StringType in text converter context"),
        };

        public static char[] DeserializeCharArray(BitReader header)
        {
            if (header.State.Settings.UseUTF8)
                return header.ReadUTF8(s => new char[s], c => c.AsMemory());
            else
            {
                int size = (int)header.ReadCompressedInt();
                char[]? chArr = new char[size];

                var deserializer = header.Finish();
                deserializer.FastReadShorts(MemoryMarshal.Cast<char, short>(chArr.AsSpan()));

                return chArr;
            }
        }

        public static StringBuilder DeserializeStringBuilder(BitReader header) => new StringBuilder(header.ReadString());

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
