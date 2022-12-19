using System;

namespace TinkState.Model
{
	/// <summary>
	/// Use Observable or State backing field for the property.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class ObservableAttribute : Attribute {}
}
