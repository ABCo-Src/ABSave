using System;
using System.Runtime.CompilerServices;

namespace ABCo.ABSave.Mapping.Description.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class SaveAttribute : AttributeWithVersion
    {
        public int Order;

        public SaveAttribute([CallerLineNumber] int order = 0)
            => Order = order;
    }
}
