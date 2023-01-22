using System.Collections;
using System.Collections.Generic;

namespace TinkState.Internal
{
	class ObservableDictionary<TKey, TValue> : Dispatcher, TinkState.ObservableDictionary<TKey, TValue>,
		DispatchingObservable<ObservableDictionary<TKey, TValue>>
	{
		readonly Dictionary<TKey, TValue> entries;
		TinkState.Stream<ObservableDictionaryChange<TKey, TValue>> changes;
		bool valid;

		public ObservableDictionary()
		{
			entries = new Dictionary<TKey, TValue>();
		}

		public TinkState.Stream<ObservableDictionaryChange<TKey, TValue>> Changes
		{
			get
			{
				// TODO: maybe have a callback to nullify this when the last stream binding is disposed?
				return changes ??= Observable.Stream<ObservableDictionaryChange<TKey, TValue>>();
			}
		}

		public TValue this[TKey key]
		{
			get
			{
				Calculate();
				return entries[key];
			}
			set
			{
				if (changes == null)
				{
					entries[key] = value;
				}
				else
				{
					if (entries.TryGetValue(key, out var oldValue))
					{
						entries[key] = value;
						DispatchReplace(key, oldValue, value);
					}
					else
					{
						entries[key] = value;
						DispatchAdd(key, value);
					}
				}
				Invalidate();
			}
		}

		void DispatchAdd(TKey key, TValue value)
		{
			changes.Dispatch(new ObservableDictionaryChange<TKey, TValue>(ObservableDictionaryChangeKind.Add, key, default, value));
		}

		void DispatchRemove(TKey key, TValue oldValue)
		{
			changes.Dispatch(new ObservableDictionaryChange<TKey, TValue>(ObservableDictionaryChangeKind.Remove, key, oldValue, default));
		}

		void DispatchReplace(TKey key, TValue oldValue, TValue newValue)
		{
			// TODO: don't dispatch if the value is the same? but we need a comparer for this...
			changes.Dispatch(new ObservableDictionaryChange<TKey, TValue>(ObservableDictionaryChangeKind.Replace, key, oldValue, newValue));
		}

		public ICollection<TKey> Keys
		{
			get
			{
				Calculate();
				return entries.Keys;
			}
		}

		public ICollection<TValue> Values
		{
			get
			{
				Calculate();
				return entries.Values;
			}
		}

		public int Count
		{
			get
			{
				Calculate();
				return entries.Count;
			}
		}

		public bool IsReadOnly => false;

		public void Add(TKey key, TValue value)
		{
			entries.Add(key, value);
			if (changes != null) DispatchAdd(key, value);
			Invalidate();
		}

		void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
		{
			((ICollection<KeyValuePair<TKey, TValue>>) entries).Add(item);
			if (changes != null) DispatchAdd(item.Key, item.Value);
			Invalidate();
		}

		public void Clear()
		{
			entries.Clear();
			// TODO: dispatch clear
			Invalidate();
		}

		bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
		{
			Calculate();
			return ((ICollection<KeyValuePair<TKey, TValue>>) entries).Contains(item);
		}

		public bool ContainsKey(TKey key)
		{
			Calculate();
			return entries.ContainsKey(key);
		}

		void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			Calculate();
			((ICollection<KeyValuePair<TKey, TValue>>) entries).CopyTo(array, arrayIndex);
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			Calculate();
			return entries.GetEnumerator();
		}

		public bool Remove(TKey key)
		{
			bool itemRemoved;
			if (changes == null)
			{
				itemRemoved = entries.Remove(key);
				if (itemRemoved)
				{
					Invalidate();
				}
			}
			else
			{
				if (entries.TryGetValue(key, out var oldValue))
				{
					itemRemoved = true;
					entries.Remove(key);
					DispatchRemove(key, oldValue);
				}
				else
				{
					itemRemoved = false;
				}
			}

			return itemRemoved;
		}

		bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
		{
			var itemRemoved = ((ICollection<KeyValuePair<TKey, TValue>>) entries).Remove(item);
			if (itemRemoved)
			{
				// TODO
				Invalidate();
			}

			return itemRemoved;
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			Calculate();
			return entries.TryGetValue(key, out value);
		}

		IEqualityComparer<ObservableDictionary<TKey, TValue>> DispatchingObservable<ObservableDictionary<TKey, TValue>>.GetComparer()
		{
			return NeverEqualityComparer<ObservableDictionary<TKey, TValue>>.Instance;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			Calculate();
			return entries.GetEnumerator();
		}

		long DispatchingObservable.GetRevision()
		{
			return revision;
		}

		public ObservableDictionary<TKey, TValue> GetCurrentValue()
		{
			return this;
		}

		void Calculate()
		{
			valid = true;
			AutoObservable.Track(this);
		}

		void Invalidate()
		{
			if (valid)
			{
				valid = false;
				Fire();
			}
		}
	}
}