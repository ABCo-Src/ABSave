using ABCo.ABSave.Configuration;
using ABCo.ABSave.Converters;
using ABCo.ABSave.Deserialization;
using ABCo.ABSave.Exceptions;
using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping;
using ABCo.ABSave.Mapping.Description;
using ABCo.ABSave.Mapping.Description.Attributes;
using ABCo.ABSave.Mapping.Generation.Inheritance;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace ABCo.ABSave.Serialization
{
    /// <summary>
    /// The central object that everything in ABSave writes to. Provides facilties to write primitive types, including strings.
    /// </summary>
    public sealed partial class ABSaveSerializer : IDisposable
    {
        public Stream Output { get; private set; } = null!;

        public SerializeCurrentState State { get; }

        readonly BitWriter _currentBitWriter;

        internal ABSaveSerializer(ABSaveMap map)
        {
            Output = null!;
            State = new SerializeCurrentState(map);
            _currentBitWriter = new BitWriter(this);
        }

        public void Initialize(Stream output, Dictionary<Type, uint>? targetVersions)
        {
            if (!output.CanWrite)
                throw new Exception("Cannot use unwriteable stream.");

            Output = output;
            State.TargetVersions = targetVersions;

            Reset();
        }

        public void Reset() => State.Reset();
        public void Dispose() => State.Map.ReleaseSerializer(this);

        public void SerializeRoot(object? obj) => WriteItem(obj, State.Map._rootItem);

        public void WriteItem(object? obj, MapItemInfo item)
        {
            using var writer = GetHeader();
            writer.WriteItem(obj, item);
        }

        public void WriteExactNonNullItem(object? obj, MapItemInfo item)
        {
            using var writer = GetHeader();
            writer.WriteExactNonNullItem(obj!, item);
        }

        public BitWriter GetHeader()
        {
            _currentBitWriter.SetupHeader();
            return _currentBitWriter;
        }
    }
}
