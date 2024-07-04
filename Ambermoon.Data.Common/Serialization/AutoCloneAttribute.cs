using System;

namespace Ambermoon.Data.Serialization
{
	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	public sealed class AutoCloneAttribute : System.Attribute
	{
	}

	public interface IAutoClone<T>
	{
		T DeepClone();
	}
}
