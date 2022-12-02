using System.Collections;
using System.Collections.Generic;

namespace TinkState.Internal
{
	class ObservableDictionary<TKey, TValue> : Dispatcher, TinkState.ObservableDictionary<TKey, TValue>, DispatchingObservable<ObservableDictionary<TKey, TValue>>
	{
		readonly Dictionary<TKey, TValue> entries;
		bool valid;

		public ObservableDictionary()
		{
			entries = new Dictionary<TKey, TValue>();
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
				entries[key] = value;
				Invalidate();
			}
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
			Invalidate();
		}

		void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
		{
			((ICollection<KeyValuePair<TKey, TValue>>)entries).Add(item);
			Invalidate();
		}

		public void Clear()
		{
			entries.Clear();
			Invalidate();
		}

		bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
		{
			Calculate();
			return ((ICollection<KeyValuePair<TKey, TValue>>)entries).Contains(item);
		}

		public bool ContainsKey(TKey key)
		{
			Calculate();
			return entries.ContainsKey(key);
		}

		void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			Calculate();
			((ICollection<KeyValuePair<TKey, TValue>>)entries).CopyTo(array, arrayIndex);
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			Calculate();
			return entries.GetEnumerator();
		}

		public bool Remove(TKey key)
		{
			var itemRemoved = entries.Remove(key);
			if (itemRemoved)
			{
				Invalidate();
			}
			return itemRemoved;
		}

		bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
		{
			var itemRemoved = ((ICollection<KeyValuePair<TKey, TValue>>)entries).Remove(item);
			if (itemRemoved)
			{
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

		ObservableDictionary<TKey, TValue> DispatchingObservable<ObservableDictionary<TKey, TValue>>.GetCurrentValue()
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
