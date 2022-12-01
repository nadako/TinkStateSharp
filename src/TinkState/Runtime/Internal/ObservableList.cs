using System;
using System.Collections;
using System.Collections.Generic;

namespace TinkState.Internal
{
	class ObservableList<T> : Dispatcher, TinkState.ObservableList<T>, ObservableImpl<ObservableList<T>>, Observable<IReadOnlyList<T>>, ObservableImpl<IReadOnlyList<T>>
	{
		readonly List<T> entries;
		bool valid = false;

		public T this[int index]
		{
			get
			{
				Calculate();
				return entries[index];
			}
			set
			{
				entries[index] = value;
				Invalidate();
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

		public ObservableList()
		{
			entries = new List<T>();
		}

		public ObservableList(IEnumerable<T> initial)
		{
			entries = new List<T>(initial);
		}

		public void Add(T item)
		{
			entries.Add(item);
			Invalidate();
		}

		public void Clear()
		{
			entries.Clear();
			Invalidate();
		}

		public bool Contains(T item)
		{
			Calculate();
			return entries.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			Calculate();
			entries.CopyTo(array, arrayIndex);
		}

		public Observable<IReadOnlyList<T>> Observe()
		{
			Calculate(); // hm, do we want a full calculate with tracking?
			return this;
		}

		public IEnumerator<T> GetEnumerator()
		{
			Calculate();
			return entries.GetEnumerator();
		}

		public int IndexOf(T item)
		{
			Calculate();
			return entries.IndexOf(item);
		}

		public void Insert(int index, T item)
		{
			entries.Insert(index, item);
			Invalidate();
		}

		public bool Remove(T item)
		{
			var itemRemoved = entries.Remove(item);
			if (itemRemoved)
			{
				Invalidate();
			}
			return itemRemoved;
		}

		public void RemoveAt(int index)
		{
			entries.RemoveAt(index);
			Invalidate();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			Calculate();
			return entries.GetEnumerator();
		}

		void Calculate()
		{
			valid = true;
			AutoObservable.Track((ObservableImpl<ObservableList<T>>)this);
		}

		void Invalidate()
		{
			if (valid)
			{
				valid = false;
				Fire();
			}
		}

		ObservableList<T> ObservableImpl<ObservableList<T>>.GetValueUntracked()
		{
			return this;
		}

		IEqualityComparer<ObservableList<T>> ObservableImpl<ObservableList<T>>.GetComparer()
		{
			return NeverEqualityComparer<ObservableList<T>>.Instance;
		}

		long ObservableImpl.GetRevision()
		{
			return revision;
		}

		IReadOnlyList<T> Observable<IReadOnlyList<T>>.Value => AutoObservable.Track((ObservableImpl<IReadOnlyList<T>>)this);

		IDisposable Observable<IReadOnlyList<T>>.Bind(Action<IReadOnlyList<T>> callback, IEqualityComparer<IReadOnlyList<T>> comparer, Scheduler scheduler)
		{
			return Binding<IReadOnlyList<T>>.Create(this, callback, comparer, scheduler);
		}

		IEqualityComparer<IReadOnlyList<T>> ObservableImpl<IReadOnlyList<T>>.GetComparer()
		{
			return NeverEqualityComparer<IReadOnlyList<T>>.Instance;
		}

		IReadOnlyList<T> ObservableImpl<IReadOnlyList<T>>.GetValueUntracked()
		{
			return entries;
		}
	}
}
