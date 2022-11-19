using System.Collections.Generic;

namespace TinkState
{
	/// <summary>
	/// A observable collection of values that can be individually accessed by index.
	/// </summary>
	/// <remarks>
	/// This is an observable wrapper around <see cref="List{T}"/> that lets auto-Observables track
	/// any changes to the collection.
	/// </remarks>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	public interface ObservableList<T> : IList<T>
	{
	}
}
