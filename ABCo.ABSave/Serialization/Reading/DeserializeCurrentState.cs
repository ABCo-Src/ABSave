using ABCo.ABSave.Mapping;
using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Serialization.Reading
{
    public class DeserializeCurrentState : CurrentState
    {
        public DeserializeCurrentState(ABSaveMap map) : base(map) { }
        internal List<Type> CachedKeys = new List<Type>();
    }
}
