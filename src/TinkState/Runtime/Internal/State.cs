﻿using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TinkState.Internal
{
	class State<T> : Dispatcher, TinkState.State<T>, DispatchingObservable<T>
	{
		readonly IEqualityComparer<T> comparer;
		T value;

		public State(T value, IEqualityComparer<T> comparer)
		{
			this.value = value;
			this.comparer = comparer ?? EqualityComparer<T>.Default;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public T Value
		{
			get => AutoObservable.Track(this);
			set
			{
				// TODO: warn if called from inside bindings, add a way to run atomically
				if (!comparer.Equals(value, this.value))
				{
					this.value = value;
					Fire();
				}
			}
		}

		public IDisposable Bind(Action<T> callback, IEqualityComparer<T> comparer = null, Scheduler scheduler = null)
		{
			return new Binding<T>(this, callback, comparer, scheduler);
		}

		T DispatchingObservable<T>.GetCurrentValue()
		{
			return value;
		}

		IEqualityComparer<T> DispatchingObservable<T>.GetComparer()
		{
			return comparer;
		}

		long DispatchingObservable.GetRevision()
		{
			return revision;
		}
	}
}