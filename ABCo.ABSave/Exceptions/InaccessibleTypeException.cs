using System;

namespace ABCo.ABSave.Exceptions
{
	public class InaccessibleTypeException : ABSaveException
	{
		public InaccessibleTypeException(Type type) : base($"The provided type '{type.Name}' is inaccessible. The type should be declared as public.")
		{ }
	}
}