using ABCo.ABSave.Configuration;
using ABCo.ABSave.Serialization.Converters;
using ABCo.ABSave.Serialization.Writing.Reading;
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
        public ABSaveMap Map { get; }
        public ABSaveSettings Settings { get; }
        public bool ShouldReverseEndian { get; }

        CachedConverterDetails[] _cachedConverterDetails = null!;
        byte[]? _stringBuffer;

        internal CurrentState(ABSaveMap map)
        {
            Map = map;
            Settings = map.Settings;
            ShouldReverseEndian = map.Settings.UseLittleEndian != BitConverter.IsLittleEndian;

            _cachedConverterDetails = new CachedConverterDetails[map._highestConverterInstanceId];            
        }

        internal void Reset() => Array.Clear(_cachedConverterDetails, 0, _cachedConverterDetails.Length);

        internal CachedConverterDetails GetCachedDetails(Converter item)
        {
            if (item._instanceId >= _cachedConverterDetails.Length)
                Array.Resize(ref _cachedConverterDetails, (int)Map._highestConverterInstanceId);

            return _cachedConverterDetails[item._instanceId];
        }

        internal VersionInfo CreateNewCache(Converter item, uint version)
        {
            VersionInfo newInfo = Map.GetVersionInfo(item, version);
            _cachedConverterDetails[item._instanceId].CurrentInfo = newInfo;
            return newInfo;
        }

        internal VersionInfo AddKeyIdToCache(Converter item, int id) => _cachedConverterDetails[item._instanceId].CurrentInfo = newInfo;

        internal byte[] GetStringBuffer(int length)
        {
            if (_stringBuffer == null || _stringBuffer.Length < length)
                return _stringBuffer = ABSaveUtils.CreateUninitializedArray<byte>(length);

            else return _stringBuffer;
        }

        public MapItemInfo GetRuntimeMapItem(Type type) => Map.GetRuntimeMapItem(type);
    }
}