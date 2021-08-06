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
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace ABCo.ABSave.Serialization
{
    /// <summary>
    /// The central object that everything in ABSave writes to. Provides facilties to write primitive types, including strings.
    /// </summary>
    public sealed partial class ABSaveSerializer : IDisposable
    {
        public Dictionary<Type, uint>? TargetVersions { get; private set; }
        public Stream Output { get; private set; } = null!;

        public CurrentState State { get; }

        internal ABSaveSerializer(ABSaveMap map)
        {
            Output = null!;
            State = new CurrentState(map);
        }

        public void Initialize(Stream output, Dictionary<Type, uint>? targetVersions)
        {
            if (!output.CanWrite)
                throw new Exception("Cannot use unwriteable stream.");

            Output = output;
            TargetVersions = targetVersions;

            Reset();
        }

        public void Reset() => State.Reset();
        public void Dispose() => State.Map.ReleaseSerializer(this);

        public void SerializeRoot(object? obj) => SerializeItem(obj, State.Map._rootItem);

        public void SerializeItem(object? obj, MapItemInfo item)
        {
            if (obj == null)
                WriteByte(0);

            else
            {
                var currentHeader = new BitTarget(this);
                ItemSerializer.SerializeItem(obj, item, ref currentHeader);
            }
        }

        public void SerializeItem(object? obj, MapItemInfo item, ref BitTarget header)
        {
            if (obj == null)
            {
                header.WriteBitOff();
                header.Apply();
            }

            else ItemSerializer.SerializeItem(obj, item, ref header);
        }

        public void SerializeExactNonNullItem(object obj, MapItemInfo item)
        {
            var currentHeader = new BitTarget(this);
            ItemSerializer.SerializeExactNonNullItem(obj, item, ref currentHeader);
        }
    }
}
