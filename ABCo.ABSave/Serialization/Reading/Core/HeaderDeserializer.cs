﻿using ABCo.ABSave.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Serialization.Reading.Core
{
    internal static class HeaderDeserializer
    {
        public static void ReadHeader(ABSaveDeserializer deserializer)
        {
            if (!deserializer.State.Settings.IncludeVersioningHeader) return;
            deserializer.State.IncludeVersioningInfo = deserializer.ReadBit();
        }
    }
}
