using System;
using System.Collections.Generic;

namespace TinkState.Internal
{
	class TransformObservable<TIn, TOut> : DispatchingObservable<TOut>, Observable<TOut>
	{
		readonly DispatchingObservable<TIn> source;
		readonly Func<TIn, TOut> transform;
		readonly IEqualityComparer<TOut> comparer;
		long lastTransformedRevision = -1;
		TOut last;

		public TransformObservable(DispatchingObservable<TIn> source, Func<TIn, TOut> transform, IEqualityComparer<TOut> comparer)
		{
			this.source = source;
			this.transform = transform;
			this.comparer = comparer ?? EqualityComparer<TOut>.Default;
		}

		public long GetRevision()
		{
			return source.GetRevision();
		}

		public bool CanFire()
		{
			return source.CanFire();
		}

		public void Subscribe(Observer observer)
		{
			source.Subscribe(observer);
		}

		public void Unsubscribe(Observer observer)
		{
			source.Unsubscribe(observer);
		}

		public TOut GetCurrentValue()
		{
			var sourceRevision = source.GetRevision();
			if (sourceRevision > lastTransformedRevision)
			{
				lastTransformedRevision = sourceRevision;
				last = transform(source.GetCurrentValue());
			}

			return last;
		}

		public IEqualityComparer<TOut> GetComparer()
		{
			return comparer;
		}

		public TOut Value => AutoObservable.Track(this);

		public IDisposable Bind(Action<TOut> callback, IEqualityComparer<TOut> comparer = null, Scheduler scheduler = null)
		{
			return Binding<TOut>.Create(this, callback, comparer, scheduler);
		}

		public Observable<TOutNew> Map<TOutNew>(Func<TOut, TOutNew> transform, IEqualityComparer<TOutNew> comparer = null)
		{
			return TransformObservable.Create(this, transform, comparer);
		}
	}

	class OneTimeTransformObservable<TIn, TOut> : Observable<TOut>, ValueProvider<TOut>
	{
		readonly ValueProvider<TIn> source;
		readonly Func<TIn, TOut> transform;
		TOut value;
		bool computed;

		public OneTimeTransformObservable(ValueProvider<TIn> source, Func<TIn, TOut> transform)
		{
			this.source = source;
			this.transform = transform;
		}

		public TOut Value => GetCurrentValue();

		public TOut GetCurrentValue()
		{
			if (!computed)
			{
				computed = true;
				value = transform(source.GetCurrentValue());
			}

			return value;
		}

		public IDisposable Bind(Action<TOut> callback, IEqualityComparer<TOut> comparer = null, Scheduler scheduler = null)
		{
			callback.Invoke(Value);
			return NoopDisposable.Instance;
		}

		public Observable<TOutNew> Map<TOutNew>(Func<TOut, TOutNew> transform, IEqualityComparer<TOutNew> comparer = null)
		{
			return new OneTimeTransformObservable<TOut, TOutNew>(this, transform);
		}
	}

	static class TransformObservable
	{
		public static Observable<TOut> Create<TIn, TOut>(DispatchingObservable<TIn> observable, Func<TIn, TOut> transform, IEqualityComparer<TOut> comparer)
		{
			if (observable.CanFire())
			{
				return new TransformObservable<TIn, TOut>(observable, transform, comparer);
			}
			else
			{
				return new OneTimeTransformObservable<TIn, TOut>(observable, transform);
			}
		}
	}
}