using ABCo.ABSave.Configuration;
using ABCo.ABSave.Serialization.Converters;
using ABCo.ABSave.Serialization.Reading;
using ABCo.ABSave.Mapping;
using System;
using System.Collections.Generic;
using System.Text;
using ABCo.ABSave.Helpers;

namespace ABCo.ABSave.Serialization
{
    /// <summary>
    /// Stores the current state while serializing/deserializing.
    /// </summary>
    public class CurrentState
    {
        /// <summary>
        /// The current map
        /// </summary>
        public ABSaveMap Map { get; }

        /// <summary>
        /// The current settings
        /// </summary>
        public ABSaveSettings Settings { get; }

        /// <summary>
        /// Whether the target endianness for the ABSave document.
        /// </summary>
        public bool ShouldReverseEndian { get; }

        /// <summary>
        /// Denotes whether versioning info is present or not.
        /// </summary>
        public bool IncludeVersioningInfo { get; internal set; }

        CachedConverterDetails[] _cachedConverterDetails = null!;
        byte[]? _stringBuffer;
        int _currentSubTypeCacheCount;

        internal CurrentState(ABSaveMap map)
        {
            Map = map;
            Settings = map.Settings;
            ShouldReverseEndian = map.Settings.UseLittleEndian != BitConverter.IsLittleEndian;

            _cachedConverterDetails = new CachedConverterDetails[map._highestConverterInstanceId];            
        }

        internal void Reset()
        {
            Array.Clear(_cachedConverterDetails, 0, _cachedConverterDetails.Length);
            _currentSubTypeCacheCount = 0;
        }

        internal VersionInfo GetCachedInfo(Converter item)
        {
            EnsureConverterInCachedDetails(item);
            return _cachedConverterDetails[item._instanceId].CurrentInfo;
        }

        internal VersionInfo CreateNewCache(Converter item, uint version)
        {
            VersionInfo newInfo = Map.GetVersionInfo(item, version);
            _cachedConverterDetails[item._instanceId].CurrentInfo = newInfo;
            return newInfo;
        }

        internal int? GetCachedKeyInfo(Converter item)
        {
            EnsureConverterInCachedDetails(item);
            return _cachedConverterDetails[item._instanceId].KeyInheritanceCachedValue;
        }

        private void EnsureConverterInCachedDetails(Converter item)
        {
            if (item._instanceId >= _cachedConverterDetails.Length)
                Array.Resize(ref _cachedConverterDetails, (int)Map._highestConverterInstanceId);
        }

        internal void AddNewKeyCacheNumber(Converter item) =>
            _cachedConverterDetails[item._instanceId].KeyInheritanceCachedValue = _currentSubTypeCacheCount++;

        internal byte[] GetStringBuffer(int length)
        {
            if (_stringBuffer == null || _stringBuffer.Length < length)
                return _stringBuffer = ABSaveUtils.CreateUninitializedArray<byte>(length);

            else return _stringBuffer;
        }

        public MapItemInfo GetRuntimeMapItem(Type type) => Map.GetRuntimeMapItem(type);
    }
}