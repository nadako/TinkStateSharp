using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TinkState.Internal
{
	class ExternalObservableSource<T> : Dispatcher, TinkState.ExternalObservableSource<T>, DispatchingObservable<T>
	{
		public event Action Subscribed;
		public event Action Unsubscribed;

		readonly Func<T> getter;
		readonly IEqualityComparer<T> comparer;

		bool isSubscribedTo;
		bool valid;
		T last;

		public ExternalObservableSource(Func<T> getter, IEqualityComparer<T> comparer)
		{
			this.getter = getter;
			this.comparer = comparer ?? EqualityComparer<T>.Default;
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
			return comparer;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public T Value => AutoObservable.Track(this);

		public void Invalidate()
		{
			if (valid)
			{
				valid = false;
				Fire();
			}
		}

		public T GetCurrentValue()
		{
			if (!valid || !isSubscribedTo)
			{
				Calculate();
			}
			return last;
		}

		public long GetRevision()
		{
			// TODO: auto-observables rely on this so what should we do?
			return revision;
		}

		void Calculate()
		{
			last = getter();
			valid = true;
		}

		protected override void OnStatusChange(bool active)
		{
			if (active) WakeUp(); else Sleep();
		}

		void WakeUp()
		{
			isSubscribedTo = true;
			Calculate();
			Subscribed?.Invoke();
		}

		void Sleep()
		{
			isSubscribedTo = false;
			Unsubscribed?.Invoke();
		}
	}
}
