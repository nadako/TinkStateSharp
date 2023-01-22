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
		Stream<ObservableDictionaryChange<TKey,TValue>> Changes { get; }
	}

	public enum ObservableDictionaryChangeKind
	{
		Add,
		Remove,
		Replace
	}

	public class ObservableDictionaryChange<TKey,TValue>
	{
		public readonly ObservableDictionaryChangeKind Kind;
		public readonly TKey Key;
		public readonly TValue OldValue;
		public readonly TValue NewValue;

		public ObservableDictionaryChange(ObservableDictionaryChangeKind kind, TKey key, TValue oldValue, TValue newValue)
		{
			Kind = kind;
			Key = key;
			OldValue = oldValue;
			NewValue = newValue;
		}
	}
}
