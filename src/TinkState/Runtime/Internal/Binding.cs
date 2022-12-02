using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace TinkState.Internal
{
	class Binding<T> : IDisposable, Observer, Schedulable
	{
		enum Status
		{
			Valid,
			Invalid,
			Disposed,
		}

		ObservableImpl<T> data;
		Action<T> callback;
		IEqualityComparer<T> comparer;
		readonly Scheduler scheduler;

		Status status;
		T last;

		internal Binding(ObservableImpl<T> data, Action<T> callback, IEqualityComparer<T> comparer, Scheduler scheduler)
		{
			this.data = data;
			this.callback = callback;
			this.scheduler = scheduler ?? Observable.Scheduler;
			this.comparer = CombinedComparer<T>.Create(data.GetComparer(), comparer);

			data.Subscribe(this);
			callback.Invoke(last = AutoObservable.Untracked(data));
		}

		public void Notify()
		{
			if (status == Status.Valid)
			{
				status = Status.Invalid;
				scheduler.Schedule(this);
			}
		}

		public void Dispose()
		{
			if (status != Status.Disposed)
			{
				status = Status.Disposed;
				data.Unsubscribe(this);
				data = null;
				callback = null;
				comparer = null;
			}
		}

		public void Run()
		{
			switch (status)
			{
				case Status.Disposed:
				case Status.Valid:
					break; // TODO: can this ever happen?
				case Status.Invalid:
					status = Status.Valid;

					var prev = last;
					var next = last = data.GetValueUntracked();

					var canFire = data.CanFire();

					if (!comparer.Equals(prev, next))
					{
						callback.Invoke(next);
					}

					if (!canFire)
					{
						// Auto-observables can become inactive (can't fire anymore) if their last computation
						// doesn't subscribe to any other observables. In such cases we can also dispose the binding
						// as it won't ever trigger anymore
						Dispose();
					}
					break;
			}
		}
	}

	class CombinedComparer<T> : IEqualityComparer<T>
	{
		public static IEqualityComparer<T1> Create<T1>(IEqualityComparer<T1> c1, IEqualityComparer<T1> c2)
		{
			if (c1 == null) return c2;
			if (c2 == null) return c1;
			return new CombinedComparer<T1>(c1, c2);
		}

		readonly IEqualityComparer<T> c1;
		readonly IEqualityComparer<T> c2;

		CombinedComparer(IEqualityComparer<T> c1, IEqualityComparer<T> c2)
		{
			this.c1 = c1;
			this.c2 = c2;
		}

		public bool Equals(T x, T y)
		{
			return c1.Equals(x, y) || c2.Equals(x, y);
		}

		[ExcludeFromCodeCoverage] // never used by us and the class is not public
		public int GetHashCode(T obj)
		{
			return c1.GetHashCode();
		}
	}
}
