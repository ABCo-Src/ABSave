using System;

namespace ABSoftware.ABSave.Converters
{
    public class GuidTypeConverter : ABSaveTypeConverter
    {
        public static GuidTypeConverter Instance = new GuidTypeConverter();
        private GuidTypeConverter() { }

        public override bool HasNonExactTypes => false;
        public override Type[] ExactTypes { get; } = new Type[] { typeof(Guid) };

        public override void Serialize(object obj, Type type, ABSaveWriter writer) => writer.WriteByteArray(((Guid)obj).ToByteArray(), false);
        public override object Deserialize(Type type, ABSaveReader reader)
        {
            var data = new byte[16];
            reader.ReadBytes(data);
            return new Guid(data);
        }
    }
}
