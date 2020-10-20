using System;

namespace ABSoftware.ABSave.Converters
{
    public class NumberTypeConverter : ABSaveTypeConverter
    {
        // Enums get the TypeCode of Int32.
        public static NumberTypeConverter Instance = new NumberTypeConverter();
        private NumberTypeConverter() { }

        public override bool HasNonExactTypes => false;

        public override Type[] ExactTypes { get; } = new Type[]
        {
            typeof(byte),
            typeof(sbyte),
            typeof(char), 
            typeof(ushort),
            typeof(short),
            typeof(uint), 
            typeof(int),
            typeof(ulong),
            typeof(long), 
            typeof(float),
            typeof(double),
            typeof(decimal)
        };

        public override void Serialize(object obj, Type type, ABSaveWriter writer) => writer.WriteNumber(obj, Type.GetTypeCode(type));
        public override object Deserialize(Type type, ABSaveReader reader) => reader.ReadNumber(Type.GetTypeCode(type));
    }
}
