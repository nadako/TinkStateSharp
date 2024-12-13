using System;
using System.Collections.Generic;

namespace TinkState.Internal
{
	partial class ObservableDictionary<TKey, TValue> : Observable<IReadOnlyDictionary<TKey, TValue>>, DispatchingObservable<IReadOnlyDictionary<TKey, TValue>>
	{
		IReadOnlyDictionary<TKey, TValue> Observable<IReadOnlyDictionary<TKey, TValue>>.Value
		{
			get => AutoObservable.Track<ObservableDictionary<TKey, TValue>>(this).entries;
		}

		IDisposable Observable<IReadOnlyDictionary<TKey, TValue>>.Bind(Action<IReadOnlyDictionary<TKey, TValue>> callback, IEqualityComparer<IReadOnlyDictionary<TKey, TValue>> comparer, Scheduler scheduler)
		{
			return new Binding<IReadOnlyDictionary<TKey, TValue>>(this, callback, comparer, scheduler);
		}

		Observable<TOut> Observable<IReadOnlyDictionary<TKey, TValue>>.Map<TOut>(Func<IReadOnlyDictionary<TKey, TValue>, TOut> transform, IEqualityComparer<TOut> comparer)
		{
			return new TransformObservable<IReadOnlyDictionary<TKey, TValue>, TOut>(this, transform, comparer);
		}

		IReadOnlyDictionary<TKey, TValue> ValueProvider<IReadOnlyDictionary<TKey, TValue>>.GetCurrentValue()
		{
			valid = true;
			return entries;
		}

		IEqualityComparer<IReadOnlyDictionary<TKey, TValue>> DispatchingObservable<IReadOnlyDictionary<TKey, TValue>>.GetComparer()
		{
			return NeverEqualityComparer<IReadOnlyDictionary<TKey, TValue>>.Instance;
		}
	}
}
