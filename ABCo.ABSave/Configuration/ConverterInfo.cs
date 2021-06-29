using System;

namespace ABCo.ABSave.Configuration
{
    /// <summary>
    /// Contains information about a given converter.
    /// </summary>
    internal class ConverterInfo
    {
        public Type ConverterType { get; }

        /// <summary>
        /// The unique ID given to this converter, used inside the MapGenerator to store a cache of converter instances.
        /// </summary>
        public int ConverterId { get; }

        public ConverterInfo(Type type, int converterId)
        {
            ConverterType = type;
            ConverterId = converterId;
        }
    }
}
