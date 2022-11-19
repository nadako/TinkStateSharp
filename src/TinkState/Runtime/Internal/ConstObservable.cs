using System;
using System.Collections.Generic;

namespace TinkState.Internal
{
	class ConstObservable<T> : Observable<T>
	{
		public T Value { get; }

		public ConstObservable(T value)
		{
			Value = value;
		}

		public IDisposable Bind(Action<T> callback, IEqualityComparer<T> comparer = null, Scheduler scheduler = null)
		{
			// don't even bother creating Binding, simply invoke the callback and return a noop disposable
			callback.Invoke(Value);
			return NoopDisposable.Instance;
		}
	}
}