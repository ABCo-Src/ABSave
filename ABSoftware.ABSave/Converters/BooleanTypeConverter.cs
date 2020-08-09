﻿using ABSoftware.ABSave.Deserialization;
using ABSoftware.ABSave.Helpers;
using ABSoftware.ABSave.Serialization;
using System;

namespace ABSoftware.ABSave.Converters
{
    public class BooleanTypeConverter : ABSaveTypeConverter
    {
        public static BooleanTypeConverter Instance = new BooleanTypeConverter();
        private BooleanTypeConverter() { }

        public override bool HasExactType => true;
        public override Type ExactType => typeof(bool);

        public override void Serialize(object obj, TypeInformation typeInfo, ABSaveWriter writer) => writer.WriteByte((bool)obj ? (byte)1 : (byte)0);
        public override object Deserialize(TypeInformation typeInfo, ABSaveReader reader) => reader.ReadByte() > 0;
    }
}
