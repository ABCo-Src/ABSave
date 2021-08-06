using ABCo.ABSave.Configuration;
using ABCo.ABSave.Converters;
using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description;
using ABCo.ABSave.Mapping.Description.Attributes;
using ABCo.ABSave.Mapping.Generation.Inheritance;
using System;
using System.Collections.Generic;
using System.IO;

namespace ABCo.ABSave.Deserialization
{
    public sealed partial class ABSaveDeserializer : IDisposable
    {
        public Stream Source { get; private set; }
        public CurrentState State { get; private set; }

        internal ABSaveDeserializer(ABSaveMap map)
        {
            Source = null!;
            State = new CurrentState(map);
            _currentBitReader = new BitReader(this);
        }

        public void Initialize(Stream source)
        {
            Source = source;
            Reset();
        }

        public void Reset() => State.Reset();

        public BitReader GetHeader()
        {
            _currentBitReader.SetupHeader();
            return _currentBitReader;
        }

        public void Dispose() => State.Map.ReleaseDeserializer(this);
        public object? DeserializeRoot() => GetHeader().ReadItem(State.Map._rootItem);

        public object? ReadItem(MapItemInfo info) => GetHeader().ReadItem(info);
        public object? ReadExactNonNullItem(MapItemInfo info) => GetHeader().ReadExactNonNullItem(info);
    }
}
