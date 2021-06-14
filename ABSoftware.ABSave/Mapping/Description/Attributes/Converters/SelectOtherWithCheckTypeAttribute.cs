using System;
using System.Collections.Generic;
using System.Text;

namespace ABCo.ABSave.Mapping.Description.Attributes.Converters
{
    /// <summary>
    /// Tells ABSave to use 'CheckType' on a converter if none of the other selections match what it's searching for.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SelectOtherWithCheckTypeAttribute : Attribute { }
}
