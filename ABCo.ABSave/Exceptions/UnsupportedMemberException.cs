using System.Reflection;

namespace ABCo.ABSave.Exceptions
{
    public class UnsupportedMemberException : ABSaveException
    {
	    public UnsupportedMemberException(MemberInfo member, string reason) : base($"The supplied member '{member.Name}' is not supported. Reason: {reason}")
	    { }
    }
}
