using ABCo.ABSave.Mapping.Description;
using ABCo.ABSave.Mapping.Description.Attributes;
using System;

namespace ABCo.ABSave.TestOtherAssembly
{
    [SaveMembers]
    [SaveInheritance(SaveInheritanceMode.Key)]
    public class OtherAssemblyBase { }

    [SaveMembers]
    [SaveInheritanceKey("First")]
    public class OtherAssemblySub : OtherAssemblyBase { }
}
