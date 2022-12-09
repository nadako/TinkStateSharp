using System;

namespace TinkState.Model
{
	[AttributeUsage(AttributeTargets.Class)]
	public class ModelAttribute : Attribute {}

	[AttributeUsage(AttributeTargets.Property)]
	public class ObservableAttribute : Attribute {}
}
