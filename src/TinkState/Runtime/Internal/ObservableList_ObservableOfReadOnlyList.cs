using System;
using System.Collections.Generic;

namespace TinkState.Internal
{
	partial class ObservableList<T> : Observable<IReadOnlyList<T>>, DispatchingObservable<IReadOnlyList<T>>
	{
		IReadOnlyList<T> Observable<IReadOnlyList<T>>.Value
		{
			get => AutoObservable.Track<IReadOnlyList<T>>(this);
		}

		IDisposable Observable<IReadOnlyList<T>>.Bind(Action<IReadOnlyList<T>> callback, IEqualityComparer<IReadOnlyList<T>> comparer, Scheduler scheduler)
		{
			return new Binding<IReadOnlyList<T>>(this, callback, comparer, scheduler);
		}

		Observable<TOut> Observable<IReadOnlyList<T>>.Map<TOut>(Func<IReadOnlyList<T>, TOut> transform, IEqualityComparer<TOut> comparer)
		{
			return new TransformObservable<IReadOnlyList<T>, TOut>(this, transform, comparer);
		}

		IReadOnlyList<T> ValueProvider<IReadOnlyList<T>>.GetCurrentValue()
		{
			valid = true;
			return entries;
		}

		IEqualityComparer<IReadOnlyList<T>> DispatchingObservable<IReadOnlyList<T>>.GetComparer()
		{
			return NeverEqualityComparer<IReadOnlyList<T>>.Instance;
		}
	}
}
