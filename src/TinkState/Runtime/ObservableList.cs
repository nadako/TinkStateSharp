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
		/// <summary>
		/// Return an <see cref="Observable{T}"/> object that represent a read-only version of this list.
		/// It will trigger its bindings and tracking auto-observables every time the list is changed.
		/// </summary>
		/// <remarks>
		/// The value of the returned observable is a read-only interface to the internal storage of this list,
		/// meaning that further access to its items will not be tracked by auto-observables and its contents might change over time.
		/// </remarks>
		/// <returns>An observable for tracking all list changes.</returns>
		public Observable<IReadOnlyList<T>> Observe();
	}
}
