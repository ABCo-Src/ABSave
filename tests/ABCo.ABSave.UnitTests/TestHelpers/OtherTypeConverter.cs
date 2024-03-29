﻿using ABCo.ABSave.Serialization.Converters;
using ABCo.ABSave.Serialization.Reading;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description.Attributes.Converters;
using ABCo.ABSave.Mapping.Generation.Converters;
using ABCo.ABSave.Serialization.Writing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABCo.ABSave.UnitTests.TestHelpers
{
    [Select(typeof(KeySubFirst))]
    [Select(typeof(KeySubSecond))]
    [Select(typeof(IndexKeySubIndex))]
    [Select(typeof(IndexKeySubKey))]
    [Select(typeof(ClassWithMinVersion))]
    public class OtherTypeConverter : Converter
    {
        public static bool WritesToHeader { get; set; }
        public const int OUTPUT_BYTE = 155;

        public override void Serialize(in SerializeInfo info)
        {
            if (WritesToHeader)
                info.Serializer.WriteBitOn();

            info.Serializer.WriteByte(OUTPUT_BYTE);
        }

        public override object Deserialize(in DeserializeInfo info)
        {
            if (WritesToHeader && !info.Deserializer.ReadBit()) throw new Exception("Deserialize read invalid header bit");

            if (info.Deserializer.ReadByte() != OUTPUT_BYTE) throw new Exception("Deserialize read invalid byte");

            return Activator.CreateInstance(info.ActualType);
        }
    }
}
