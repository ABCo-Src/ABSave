using ABCo.ABSave.Configuration;
using ABCo.ABSave.Serialization.Converters;
using ABCo.ABSave.Serialization.Writing.Reading;
using ABCo.ABSave.Mapping;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Helpers
{
    /// <summary>
    /// Stores the current state while serializing/deserializing.
    /// </summary>
    public class CurrentState
    {
        public ABSaveMap Map { get; }
        public ABSaveSettings Settings { get; }
        public bool ShouldReverseEndian { get; }

        VersionInfo[] _currentVersionInfos = null!;
        byte[]? _stringBuffer;

        internal CurrentState(ABSaveMap map)
        {
            Map = map;
            Settings = map.Settings;
            ShouldReverseEndian = map.Settings.UseLittleEndian != BitConverter.IsLittleEndian;

            _currentVersionInfos = new VersionInfo[map._highestConverterInstanceId];            
        }

        internal void Reset() => Array.Clear(_currentVersionInfos, 0, _currentVersionInfos.Length);

        internal VersionInfo GetExistingVersionInfo(Converter item)
        {
            if (item._instanceId >= _currentVersionInfos.Length)
                Array.Resize(ref _currentVersionInfos, (int)Map._highestConverterInstanceId);

            return _currentVersionInfos[item._instanceId];
        }

        internal VersionInfo GetNewVersionInfo(Converter item, uint version)
        {
            VersionInfo newInfo = Map.GetVersionInfo(item, version);
            _currentVersionInfos[item._instanceId] = newInfo;
            return newInfo;
        }

        internal byte[] GetStringBuffer(int length)
        {
            if (_stringBuffer == null || _stringBuffer.Length < length)
                return _stringBuffer = ABSaveUtils.CreateUninitializedArray<byte>(length);
            else return _stringBuffer;
        }

        public MapItemInfo GetRuntimeMapItem(Type type) => Map.GetRuntimeMapItem(type);
    }
}