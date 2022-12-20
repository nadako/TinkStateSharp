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
		T SubscribeTo<T>(DispatchingObservable<T> source);
	}

	static class AutoObservable
	{
		public static Derived Current;

		public static T Track<T>(DispatchingObservable<T> o)
		{
			if (Current != null && o.CanFire())
			{
				return Current.SubscribeTo(o);
			}
			else
			{
				return o.GetCurrentValue();
			}
		}

		public static R Untracked<R>(DispatchingObservable<R> o)
		{
			var before = Current;
			Current = null;
			var ret = o.GetCurrentValue();
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

	class AutoObservable<T> : Dispatcher, Observable<T>, Derived, Observer, DispatchingObservable<T>
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
		readonly Dictionary<DispatchingObservable, Subscription> dependencies = new Dictionary<DispatchingObservable, Subscription>();
		Status status;
		T last;

		#region subscription linking
		Subscription subscriptionsHead;
		Subscription subscriptionsTail;

		// TODO: do we want to pool subscription objects (maybe only for IL2CPP or something, needs research)?

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void AddSubscription(Subscription sub)
		{
			if (subscriptionsHead == null)
			{
				subscriptionsHead = subscriptionsTail = sub;
			}
			else
			{
				subscriptionsTail.Next = sub;
				sub.Prev = subscriptionsTail;
				subscriptionsTail = sub;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		Subscription RemoveSubscription(Subscription sub)
		{
			if (sub == subscriptionsHead) subscriptionsHead = sub.Next;
			if (sub == subscriptionsTail) subscriptionsTail = sub.Prev;
			if (sub.Prev != null) sub.Prev.Next = sub.Next;
			if (sub.Next != null) sub.Next.Prev = sub.Prev;
			var next = sub.Next;
			sub.Next = sub.Prev = null;
			return next;
		}
		#endregion

		public AutoObservable(Computation<T> computation, IEqualityComparer<T> comparer)
		{
			this.computation = computation;
			this.comparer = comparer ?? EqualityComparer<T>.Default;
			status = Status.Fresh;
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

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public T Value => AutoObservable.Track(this);

		public T GetCurrentValue()
		{
			return GetCurrentValue(false);
		}

		T GetCurrentValue(bool force)
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
					{
						var s = subscriptionsHead;
						while (s != null)
						{
							if (s.HasChanged())
							{
								valid = false;
								break;
								// we break early so leftover subscriptions aren't updated by their HasChanged,
								// however it's okay since they will be updated when we reuse a subscription if it's still needed
							}
							s = s.Next;
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
			var s = subscriptionsHead;
			while (s != null)
			{
				s.Used = false;
				s = s.Next;
			}

			// compute the value (will subscribe to tracked observables)
			last = AutoObservable.ComputeFor(this, computation);

			if (status == Status.Computing)
			{
				// status can become Dirty during computation if code is so side-effecty it tries to modify states we depend on,
				// so only set status to Computed if it was unchanged
				status = Status.Computed;
			}

			// disconnect subscriptions that are now unused and remove them from dependencies
			s = subscriptionsHead;
			while (s != null)
			{
				if (!s.Used)
				{
					var source = s.GetSource();
					dependencies.Remove(source);
					if (isSubscribedTo) source.Unsubscribe(this);
					s = RemoveSubscription(s);
				}
				else
				{
					s = s.Next;
				}
			}

			// if there are no subscriptions at all, we can dispose, meaning this observable can never trigger anymore
			// and will serve as constant value holder
			if (subscriptionsHead == null && !computation.IsPending()) Dispose();
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
			var s = subscriptionsHead;
			while (s != null)
			{
				if (!s.IsValid()) return false;
				s = s.Next;
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

			// activate subscriptions as we now want to be notified of tracked observable changes
			var s = subscriptionsHead;
			while (s != null)
			{
				s.GetSource().Subscribe(this);
				s = s.Next;
			}

			GetCurrentValue(true);
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

			if (status == Status.Fresh)
			{
				// it's the first ever call, calculate value which will also initialize subscriptions
				GetCurrentValue(true);
			}

			// if our revision is smaller than any of source revisions, update our revision
			var s = subscriptionsHead;
			while (s != null)
			{
				if (s.GetSource().GetRevision() > revision)
				{
					return revision = Revision.New();
				}
				s = s.Next;
			}
			return revision;
		}

		void Sleep()
		{
			computation.Sleep();
			isSubscribedTo = false;

			var s = subscriptionsHead;
			while (s != null)
			{
				s.GetSource().Unsubscribe(this);
				s = s.Next;
			}
		}

		public R SubscribeTo<R>(DispatchingObservable<R> source)
		{
			if (!dependencies.TryGetValue(source, out var v))
			{
				// not yet tracking - create a subscription and add a dependency
				var sub = new Subscription<R>(source);
				if (isSubscribedTo) source.Subscribe(this);
				dependencies[source] = sub;
				AddSubscription(sub);
				return sub.Last;
			}
			else
			{
				// already tracking...
				var sub = (Subscription<R>)v;
				if (!sub.Used)
				{
					// if marked as unused (happens during computation), mark as used again so it doesn't get removed after computation
					sub.Reuse();
					return sub.Last;
				}
				else
				{
					// tracked and used, nothing to do subscription-wise, simply return a value
					// TODO: we can probably simply return sub.last here
					return source.GetCurrentValue();
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
			if (subscriptionsHead == null) Dispose();
		}
	}

	abstract class Subscription
	{
		public Subscription Prev;
		public Subscription Next;

		public bool Used;

		public abstract DispatchingObservable GetSource();
		public abstract bool IsValid();
		public abstract bool HasChanged();
	}

	sealed class Subscription<T> : Subscription
	{
		public T Last;

		readonly DispatchingObservable<T> source;
		long lastRevision;

		public Subscription(DispatchingObservable<T> source)
		{
			this.source = source;
			lastRevision = source.GetRevision();
			Last = source.GetCurrentValue();
			Used = true;
		}

		public override DispatchingObservable GetSource()
		{
			return source;
		}

		public override bool IsValid()
		{
			return source.GetRevision() == lastRevision;
		}

		public override bool HasChanged()
		{
			var nextRevision = source.GetRevision();
			if (nextRevision == lastRevision) return false;
			lastRevision = nextRevision;
			var before = Last;
			Last = source.GetCurrentValue();
			return !source.GetComparer().Equals(Last, before);
		}

		public void Reuse()
		{
			Used = true;
			Last = source.GetCurrentValue();
			lastRevision = source.GetRevision();
		}
	}
}