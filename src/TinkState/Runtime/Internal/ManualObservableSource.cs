using System;
using System.Collections.Generic;

namespace TinkState.Internal
{
	class ManualObservableSource<T> : Dispatcher, Observable<T>, TinkState.ManualObservableSource<T>, DispatchingObservable<T>
	{
		T value;

		public ManualObservableSource(T initialValue)
		{
			value = initialValue;
		}

		public T Value => AutoObservable.Track(this);

		public Observable<T> Observe()
		{
			return this;
		}

		public void Invalidate()
		{
			Fire();
		}

		public void Update(T newValue)
		{
			value = newValue;
			Fire();
		}

		public IDisposable Bind(Action<T> callback, IEqualityComparer<T> comparer = null, Scheduler scheduler = null)
		{
			return new Binding<T>(this, callback, comparer, scheduler);
		}

		public Observable<TOut> Map<TOut>(Func<T, TOut> transform, IEqualityComparer<TOut> comparer = null)
		{
			return new TransformObservable<T, TOut>(this, transform, comparer);
		}

		public IEqualityComparer<T> GetComparer()
		{
			return NeverEqualityComparer<T>.Instance;
		}

		public long GetRevision()
		{
			return revision;
		}

		public T GetCurrentValue()
		{
			return value;
		}
	}
}