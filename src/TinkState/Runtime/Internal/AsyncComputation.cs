using System;
using System.Threading;

namespace TinkState.Internal
{
	abstract class AsyncComputationBase<T> : Computation<AsyncComputeResult<T>>
	{
		AutoObservable<AsyncComputeResult<T>> owner;

		AsyncComputeTask<T> task;

		public void Init(AutoObservable<AsyncComputeResult<T>> owner)
		{
			this.owner = owner;
		}

		public AsyncComputeResult<T> GetNext()
		{
			task.CancelOnComplete();
			task = StartComputation();
			var result = task.GetResult();
			if (result.Status == AsyncComputeStatus.Loading)
			{
				// task is actually asynchronously loading
				task.OnComplete(TriggerOwner);
			}
			else
			{
				// task completed synchronously
				CleanupCompletedComputation();
			}
			return result;
		}

		public bool IsPending()
		{
			return task.GetResult().Status == AsyncComputeStatus.Loading;
		}

		// TODO: consider canceling/resubscribing to the task, but then we need to invalidate the owner,
		// so it calls computation properly on value access
		public void Sleep() { }
		public void Wakeup() { }

		void TriggerOwner()
		{
			CleanupCompletedComputation();
			owner.TriggerAsync(task.GetResult());
		}

		protected abstract AsyncComputeTask<T> StartComputation();
		protected abstract void CleanupCompletedComputation();
	}

	class AsyncComputation<T> : AsyncComputationBase<T>
	{
		readonly Func<AsyncComputeTask<T>> compute;

		public AsyncComputation(Func<AsyncComputeTask<T>> compute)
		{
			this.compute = compute;
		}

		protected override AsyncComputeTask<T> StartComputation()
		{
			return compute();
		}

		protected override void CleanupCompletedComputation()
		{
			// nothing to clean up
		}
	}

	class AsyncCancelableComputation<T> : AsyncComputationBase<T>
	{
		readonly Func<CancellationToken, AsyncComputeTask<T>> compute;

		CancellationTokenSource cancellation;

		public AsyncCancelableComputation(Func<CancellationToken, AsyncComputeTask<T>> compute)
		{
			this.compute = compute;
		}

		protected override AsyncComputeTask<T> StartComputation()
		{
			// trigger cancellation token for any previous computations
			if (cancellation != null)
			{
				cancellation.Cancel();
				cancellation.Dispose();
			}
			cancellation = new CancellationTokenSource();
			return compute(cancellation.Token);
		}

		protected override void CleanupCompletedComputation()
		{
			cancellation.Dispose();
			cancellation = null;
		}
	}
}