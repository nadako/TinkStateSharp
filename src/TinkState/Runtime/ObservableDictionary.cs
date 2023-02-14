using System.Collections.Generic;

namespace TinkState
{
	/// <summary>
	/// A observable collection of key/value pairs.
	/// </summary>
	/// <remarks>
	/// This is an observable wrapper around <see cref="Dictionary{TKey, TValue}"/> that lets auto-Observables track
	/// any changes to the collection.
	/// </remarks>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	public interface ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>
	{
		/// <summary>
		/// Return an <see cref="Observable{T}"/> object that represent a read-only version of this dictionary.
		/// It will trigger its bindings and tracking auto-observables every time the list is changed.
		/// </summary>
		/// <remarks>
		/// The value of the returned observable is a read-only interface to the internal storage of this dictionary,
		/// meaning that further access to its items will not be tracked by auto-observables and its contents might change over time.
		/// </remarks>
		/// <returns>An observable for tracking all list changes.</returns>
		public Observable<IReadOnlyDictionary<TKey, TValue>> Observe();
	}
}
