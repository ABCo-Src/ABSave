using System;

namespace ABCo.ABSave.Exceptions
{
	public class UnsupportedTypeException : ABSaveException
	{
		public UnsupportedTypeException(Type type, string reason) : base($"The supplied type '{type.Name}' is not supported. Reason: {reason}")
		{ }
	}
}