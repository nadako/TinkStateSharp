using System;

namespace TinkState.Internal
{
	interface Computation<out TResult>
	{
		TResult GetNext();
		bool IsPending();
		void Wakeup();
		void Sleep();
	}

	class SyncComputation<T> : Computation<T>
	{
		readonly Func<T> compute;

		public SyncComputation(Func<T> compute)
		{
			this.compute = compute;
		}

		public T GetNext()
		{
			return compute();
		}

		public bool IsPending()
		{
			return false;
		}

		public void Sleep()
		{
		}

		public void Wakeup()
		{
		}
	}
}