using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TinkState.Internal
{
	class ExternalObservable<T> : Dispatcher, Observable<T>, DispatchingObservable<T>
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public T Value => AutoObservable.Track(this);

		readonly Func<T> getter;
		readonly Action<Action> wakeup;
		readonly Action sleep;
		readonly IEqualityComparer<T> comparer;

		T last;
		bool isDirty;
		bool isSubscribedTo;

		public ExternalObservable(Func<T> getter, Action<Action> wakeup, Action sleep, IEqualityComparer<T> comparer)
		{
			this.getter = getter;
			this.wakeup = wakeup;
			this.sleep = sleep;
			this.comparer = comparer ?? EqualityComparer<T>.Default;
			isDirty = true;
		}

		public IDisposable Bind(Action<T> callback, IEqualityComparer<T> comparer = null, Scheduler scheduler = null)
		{
			return Binding<T>.Create(this, callback, comparer, scheduler);
		}

		public Observable<TOut> Map<TOut>(Func<T, TOut> transform, IEqualityComparer<TOut> comparer = null)
		{
			return TransformObservable.Create(this, transform, comparer);
		}

		public IEqualityComparer<T> GetComparer()
		{
			return comparer;
		}

		public long GetRevision()
		{
			return revision;
		}

		public T GetCurrentValue()
		{
			if (isDirty || !isSubscribedTo) RecalculateCurrentValue();
			return last;
		}

		void RecalculateCurrentValue()
		{
			last = getter();
			isDirty = false;
		}

		protected override void OnStatusChange(bool active)
		{
			if (active) WakeUp();
			else Sleep();
		}

		void WakeUp()
		{
			wakeup(Notify);
			isSubscribedTo = true;
			RecalculateCurrentValue();
		}

		void Sleep()
		{
			sleep();
			isSubscribedTo = false;
		}

		void Notify()
		{
			if (!isDirty)
			{
				isDirty = true;
				Fire();
			}
		}
	}
}
