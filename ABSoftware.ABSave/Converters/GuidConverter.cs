﻿using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Mapping;
using ABSoftware.ABSave.Serialization;
using System;

namespace ABSoftware.ABSave.Converters
{
    public class GuidConverter : ABSaveConverter
    {
        public static GuidConverter Instance { get; } = new GuidConverter();
        private GuidConverter() { }

        public override bool ConvertsSubTypes => true;
        public override bool AlsoConvertsNonExact => false;
        public override Type[] ExactTypes { get; } = new Type[] { typeof(Guid) };

        public override void Serialize(object obj, Type actualType, IABSaveConverterContext context, ref BitTarget header)
        {
            var guid = (Guid)obj;
            Span<byte> bytes = stackalloc byte[16];

            guid.TryWriteBytes(bytes);
            header.Serializer.WriteBytes(bytes);
        }

        public override object Deserialize(Type actualType, IABSaveConverterContext context, ref BitSource header)
        {
            Span<byte> data = stackalloc byte[16];
            header.Deserializer.ReadBytes(data);
            return new Guid(data);
        }

        public override IABSaveConverterContext TryGenerateContext(ABSaveMap map, Type type)
        {
            if (type == typeof(Guid)) return Context.Empty;
            return null;
        }

        class Context : IABSaveConverterContext
        {
            public static Context Empty = new Context();
        }
    }
}
