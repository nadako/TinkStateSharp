using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

// TODO: all the code here is pretty dirty as it was originally ported from already a pretty complicated piece
// obviously it needs some polishing and commenting

namespace TinkState.Internal
{
	interface Derived
	{
		R SubscribeTo<R>(ObservableImpl<R> source);
	}

	static class AutoObservable
	{
		public static Derived Current;

		public static T Track<T>(ObservableImpl<T> o)
		{
			if (Current != null && o.CanFire())
			{
				return Current.SubscribeTo(o);
			}
			else
			{
				return o.GetValueUntracked();
			}
		}

		public static R Untracked<R>(ObservableImpl<R> o)
		{
			var before = Current;
			Current = null;
			var ret = o.GetValueUntracked();
			Current = before;
			return ret;
		}

		public static R ComputeFor<R>(Derived o, Computation<R> computation)
		{
			var before = Current;
			Current = o;
			var ret = computation.GetNext();
			Current = before;
			return ret;
		}

		public static void ComputeFor<TStateMachine>(Derived o, in TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
		{
			var before = Current;
			Current = o;
			stateMachine.MoveNext();
			Current = before;
		}

	}

	class AutoObservable<T> : Dispatcher, Observable<T>, Derived, Observer, ObservableImpl<T>
	{
		enum Status
		{
			Fresh,
			Dirty,
			Computed,
			Computing,
		}

		readonly IEqualityComparer<T> comparer;
		readonly Computation<T> computation;
		bool isSubscribedTo;
		List<Subscription> subscriptions;
		readonly Dictionary<ObservableImpl, Subscription> dependencies = new Dictionary<ObservableImpl, Subscription>();
		Status status;
		T last;

		public AutoObservable(Computation<T> computation, IEqualityComparer<T> comparer)
		{
			this.computation = computation;
			this.comparer = comparer ?? EqualityComparer<T>.Default;
			status = Status.Fresh;
		}

		public IDisposable Bind(Action<T> callback, IEqualityComparer<T> comparer = null, Scheduler scheduler = null)
		{
			if (CanFire())
			{
				return new Binding<T>(this, callback, comparer, scheduler);
			}
			else
			{
				callback.Invoke(AutoObservable.Untracked(this));
				return NoopDisposable.Instance;
			}
		}

		public IEqualityComparer<T> GetComparer()
		{
			return comparer;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public T Value => AutoObservable.Track(this);

		public T GetValueUntracked()
		{
			return GetValueUntracked(false);
		}

		T GetValueUntracked(bool force)
		{
			const int maxIterations = 100;

			var count = 0;
			while (force || !IsValid())
			{
				force = false;
				count++;
				// TODO: test this somehow
				if (count > maxIterations) throw new Exception($"No result after {maxIterations} attempts");

				if (status == Status.Fresh)
				{
					// always compute first time
					DoCompute();
				}
				else
				{
					// check if any subscriptions has changed and only recompute if so
					var valid = true;
					foreach (var s in subscriptions)
					{
						if (s.HasChanged())
						{
							valid = false;
							break;
							// we break early so leftover subscriptions aren't updated by their HasChanged,
							// however it's okay since they will be updated when we reuse a subscription if it's still needed
						}
					}
					if (valid)
					{
						status = Status.Computed;
					}
					else
					{
						DoCompute();
					}
				}
			}
			return last;
		}

		void DoCompute()
		{
			status = Status.Computing;

			// mark current subscriptions as unused so we can later figure out which ones are still used
			// and which ones are not and remove the ones that aren't
			var prevSubs = subscriptions;
			if (prevSubs != null)
			{
				foreach (var s in prevSubs) s.Used = false;
			}

			// make a new subscription list to fill with subscriptions that will pop up during computation
			subscriptions = new List<Subscription>(); // TODO: can we avoid new list somehow?

			// compute the value (will subscribe to tracked observables)
			last = AutoObservable.ComputeFor(this, computation);

			if (status == Status.Computing)
			{
				// status can become Dirty during computation if code is so side-effecty it tries to modify states we depend on,
				// so only set status to Computed if it was unchanged
				status = Status.Computed;
			}

			// disconnect subscriptions that are now unused and remove them from dependencies
			if (prevSubs != null)
			{
				foreach (var s in prevSubs)
				{
					if (!s.Used)
					{
						dependencies.Remove(s.Source);
						if (isSubscribedTo) s.Disconnect();
					}
				}
			}

			// if there are no subscriptions at all, we can dispose, meaning this observable can never trigger anymore
			// and will serve as constant value holder
			if (subscriptions.Count == 0 && !computation.IsPending()) Dispose();
		}

		bool IsValid()
		{
			// we're "valid" (aka value is up-to-date) when we're computed,
			// however the status is only actual if we're subscribed to,
			// otherwise we have to actually check if subscriptions are valid too
			return status == Status.Computed && (isSubscribedTo || SubscriptionsValid());
		}

		bool SubscriptionsValid()
		{
			foreach (var s in subscriptions)
			{
				if (!s.IsValid()) return false;
			}

			return true;
		}

		protected override void OnStatusChange(bool active)
		{
			if (active) WakeUp(); else Sleep();
		}

		void WakeUp()
		{
			computation.Wakeup();
			isSubscribedTo = true;
			if (subscriptions != null)
			{
				// activate subscriptions as we now want to be notified of tracked observable changes
				foreach (var s in subscriptions)
				{
					s.Connect();
				}
			}
			GetValueUntracked(true);
			GetRevision();
		}

		public long GetRevision()
		{
			if (isSubscribedTo)
			{
				// revision is already up-to-date if we're subscribed to, because in this case we are also subscribed to
				// source observables and when they notify us, we fire ourselves which updates the revision
				return revision;
			}
			if (subscriptions == null)
			{
				// it's the first ever call, calculate value which will also initialize subscriptions
				GetValueUntracked();
			}
			// if our revision is smaller than any of source revisions, update our revision
			foreach (var s in subscriptions)
			{
				if (s.Source.GetRevision() > revision)
				{
					return revision = Revision.New();
				}
			}
			return revision;
		}

		void Sleep()
		{
			computation.Sleep();
			isSubscribedTo = false;
			if (subscriptions != null)
			{
				foreach (var s in subscriptions)
				{
					s.Disconnect();
				}
			}
		}

		public R SubscribeTo<R>(ObservableImpl<R> source)
		{
			if (!dependencies.TryGetValue(source, out var v))
			{
				// not yet tracking - create a subscription and add a dependency
				var sub = new Subscription<R>(source, isSubscribedTo, this);
				dependencies[source] = sub;
				subscriptions.Add(sub);
				return sub.Last;
			}
			else
			{
				// already tracking...
				var sub = (Subscription<R>)v;
				if (!sub.Used)
				{
					// if marked as unused (happens during computation), mark as used and add to subscriptions
					sub.Reuse();
					subscriptions.Add(sub);
					return sub.Last;
				}
				else
				{
					// tracked and used, nothing to do subscription-wise, simply return a value
					// TODO: we can probably simply return sub.last here
					return source.GetValueUntracked();
				}
			}
		}

		public void Notify()
		{
			switch (status)
			{
				case Status.Computed:
					// mark as dirty and notify our own subscribers
					status = Status.Dirty;
					Fire();
					break;

				case Status.Computing:
					// can happen if computation triggers state change, which is bad but possible,
					// we'll retry computing in this case
					status = Status.Dirty;
					break;
			}
		}

		public void TriggerAsync(T value)
		{
			last = value;
			Fire();
			if (subscriptions.Count == 0) Dispose();
		}
	}

	interface Subscription
	{
		ObservableImpl Source { get; }
		bool Used { get; set; }
		bool IsValid();
		bool HasChanged();
		void Connect();
		void Disconnect();
	}

	class Subscription<T> : Subscription
	{
		public ObservableImpl Source => source;
		public bool Used { get; set; }
		public T Last;

		readonly Observer owner;
		readonly ObservableImpl<T> source;
		long lastRevision;

		public Subscription(ObservableImpl<T> source, bool needsConnecting, Observer owner)
		{
			this.source = source;
			lastRevision = source.GetRevision();
			this.owner = owner;
			if (needsConnecting) Connect();
			Last = source.GetValueUntracked();
		}

		public bool IsValid()
		{
			return source.GetRevision() == lastRevision;
		}

		public bool HasChanged()
		{
			var nextRevision = source.GetRevision();
			if (nextRevision == lastRevision) return false;
			lastRevision = nextRevision;
			var before = Last;
			Last = source.GetValueUntracked();
			return !source.GetComparer().Equals(Last, before);
		}

		public void Connect()
		{
			source.Subscribe(owner);
		}

		public void Disconnect()
		{
			source.Unsubscribe(owner);
		}

		public void Reuse()
		{
			Used = true;
			Last = source.GetValueUntracked();
			lastRevision = source.GetRevision();
		}
	}
}