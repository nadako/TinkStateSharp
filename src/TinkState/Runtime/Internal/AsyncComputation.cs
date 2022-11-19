using System;

namespace TinkState.Internal
{
	class AsyncComputation<T> : Computation<AsyncComputeResult<T>>
	{
		AutoObservable<AsyncComputeResult<T>> owner;
		readonly Func<AsyncComputeTask<T>> compute;

		AsyncComputeTask<T> task;

		public AsyncComputation(Func<AsyncComputeTask<T>> compute)
		{
			this.compute = compute;
		}

		public void Init(AutoObservable<AsyncComputeResult<T>> owner)
		{
			this.owner = owner;
		}

		public AsyncComputeResult<T> GetNext()
		{
			task.CancelOnComplete();
			task = compute();
			var result = task.GetResult();
			if (result.Status == AsyncComputeStatus.Loading)
			{
				task.OnComplete(TriggerOwner);
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
			owner.TriggerAsync(task.GetResult());
		}
	}
}