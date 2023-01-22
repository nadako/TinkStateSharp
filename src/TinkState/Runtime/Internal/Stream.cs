using System;
using System.Collections.Generic;

namespace TinkState.Internal
{
	class Stream<T> : Dispatcher, TinkState.Stream<T>
	{
		internal T dispatchedItem;

		public void Dispatch(T data)
		{
			dispatchedItem = data;
			Fire();
			dispatchedItem = default;
		}

		public IDisposable Bind(Action<T> callback, Scheduler scheduler = null)
		{
			return new StreamBinding<T>(this, callback, scheduler);
		}
	}

	class StreamBinding<T> : IDisposable, Observer, Schedulable
	{
		enum Status
		{
			Unscheduled,
			Scheduled,
			Disposed,
		}

		readonly Stream<T> stream;
		Action<T> callback;
		readonly Scheduler scheduler;
		Status status;
		List<T> queue;

		internal StreamBinding(Stream<T> stream, Action<T> callback, Scheduler scheduler)
		{
			this.stream = stream;
			this.callback = callback;
			this.scheduler = scheduler ?? Observable.Scheduler;

			stream.Subscribe(this);
		}

		public void Notify()
		{
			if (status == Status.Unscheduled)
			{
				queue ??= new List<T>(1);
				queue.Add(stream.dispatchedItem);
				status = Status.Scheduled;
				scheduler.Schedule(this);
			}
		}

		public void Dispose()
		{
			if (status != Status.Disposed)
			{
				status = Status.Disposed;
				stream.Unsubscribe(this);
				callback = null;
				queue = null;
			}
		}

		public void Run()
		{
			switch (status)
			{
				case Status.Disposed:
				case Status.Unscheduled:
					break; // TODO: can this ever happen?
				case Status.Scheduled:
					status = Status.Unscheduled;

					var queue = this.queue;
					this.queue = null;

					foreach (var item in queue)
					{
						callback.Invoke(item);
					}
					break;
			}
		}
	}
}