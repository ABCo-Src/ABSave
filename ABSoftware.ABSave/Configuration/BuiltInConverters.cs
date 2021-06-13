using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Configuration
{
    public class BuiltInConverters
    {
        internal static readonly IReadOnlyDictionary<Type, Converter> BuiltInExact = new Dictionary<Type, Converter>()
        {
            { typeof(byte), PrimitiveConverter.Instance },
            { typeof(sbyte), PrimitiveConverter.Instance },
            { typeof(char), PrimitiveConverter.Instance },
            { typeof(ushort), PrimitiveConverter.Instance },
            { typeof(short), PrimitiveConverter.Instance },
            { typeof(uint), PrimitiveConverter.Instance },
            { typeof(int), PrimitiveConverter.Instance },
            { typeof(ulong), PrimitiveConverter.Instance },
            { typeof(long), PrimitiveConverter.Instance },
            { typeof(float), PrimitiveConverter.Instance },
            { typeof(double), PrimitiveConverter.Instance },
            { typeof(decimal), PrimitiveConverter.Instance },
            { typeof(IntPtr), PrimitiveConverter.Instance },
            { typeof(UIntPtr), PrimitiveConverter.Instance },
            { typeof(bool), PrimitiveConverter.Instance },
            { typeof(Guid), GuidConverter.Instance },
            { typeof(string), TextConverter.Instance },
            { typeof(StringBuilder), TextConverter.Instance },
            { typeof(char[]), TextConverter.Instance },
            { typeof(Version), VersionConverter.Instance },
            { typeof(DateTime), TickBasedConverter.Instance },
            { typeof(TimeSpan), TickBasedConverter.Instance },
            { typeof(DictionaryEntry), KeyValueConverter.Instance },
            { typeof(Type), TypeConverter.Instance },
            { typeof(Assembly), AssemblyConverter.Instance },
            { typeof(IEnumerable), EnumerableConverter.Instance },

            // Based on the most common array types in .NET, remember to update in "ArrayTypeConverter" too if changing.
            { typeof(Array), ArrayConverter.Instance },
            { typeof(byte[]), ArrayConverter.Instance },
            { typeof(string[]), ArrayConverter.Instance },
            { typeof(int[]), ArrayConverter.Instance },
        };

        internal static IReadOnlyList<Converter> BuiltInNonExact = new List<Converter>()
        {
            KeyValueConverter.Instance,
            AssemblyConverter.Instance,
            TypeConverter.Instance,
            EnumerableConverter.Instance,
            ArrayConverter.Instance,
            PrimitiveConverter.Instance,
        };
    }
}
