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
	}
}
