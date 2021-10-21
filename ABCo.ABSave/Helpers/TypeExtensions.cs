using System;

namespace ABCo.ABSave.Helpers
{
	public static class TypeExtensions
	{
		public static bool HasEmptyOrDefaultConstructor(this Type type)
		{
			return type.IsValueType || type.IsAbstract || type.GetConstructor(Type.EmptyTypes) != null;
		}
	}
}