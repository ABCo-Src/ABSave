using ABCo.ABSave.Helpers;
using ABCo.ABSave.Mapping;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Serialization
{
    public class SerializeCurrentState : CurrentState
    {
        public SerializeCurrentState(ABSaveMap map) : base(map) { }

        public Dictionary<Type, uint>? TargetVersions { get; internal set; }
    }
}
