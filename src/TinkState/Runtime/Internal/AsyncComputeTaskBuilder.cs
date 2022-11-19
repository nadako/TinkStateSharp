using System.Runtime.CompilerServices;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace TinkState.Internal
{
	interface AsyncComputeTaskSource<T>
	{
		AsyncComputeResult<T> GetResult();
		void OnComplete(Action callback);
		void CancelOnComplete();
	}

	interface AsyncComputeRunner<T>
	{
		Action MoveNext { get; }
		AsyncComputeTask<T> Task { get; }

		void SetResult(T result);
		void SetException(Exception exception);
	}

	class AsyncComputeRunner<TStateMachine, T> : AsyncComputeRunner<T>, AsyncComputeTaskSource<T> where TStateMachine : IAsyncStateMachine
	{
		public AsyncComputeTask<T> Task => new AsyncComputeTask<T>(this);
		public Action MoveNext { get; }

		readonly TStateMachine stateMachine;
		readonly Derived owner;
		Action callback;
		AsyncComputeResult<T> result;

		public AsyncComputeRunner(ref TStateMachine stateMachine, Derived owner)
		{
			MoveNext = DoMoveNext; // allocate closure right away to prevent cache checks later
			this.stateMachine = stateMachine;
			this.owner = owner;
		}

		void DoMoveNext()
		{
			AutoObservable.ComputeFor(owner, stateMachine);
		}

		public void SetResult(T result)
		{
			this.result = new AsyncComputeResult<T>(AsyncComputeStatus.Done, result, null);
			if (callback != null) callback();
		}

		public void SetException(Exception exception)
		{
			this.result = new AsyncComputeResult<T>(AsyncComputeStatus.Failed, default, exception);
			if (callback != null) callback();
		}

		public AsyncComputeResult<T> GetResult()
		{
			return result;
		}

		public void OnComplete(Action callback)
		{
			if (result.Status == AsyncComputeStatus.Loading)
			{
				this.callback = callback;
			}
			else
			{
				// this should never be called as we check for status in AsyncComputation
				callback();
			}
		}

		public void CancelOnComplete()
		{
			callback = null;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public struct AsyncComputeTaskBuilder<T>
	{
		AsyncComputeRunner<T> runner;
		T result;
		Exception exception;

		public static AsyncComputeTaskBuilder<T> Create()
		{
			return default;
		}

		public AsyncComputeTask<T> Task
		{
			get
			{
				if (runner != null)
				{
					return runner.Task;
				}
				else if (exception != null)
				{
					return AsyncComputeTask<T>.FromException(exception);
				}
				else
				{
					return AsyncComputeTask<T>.FromResult(result);
				}
			}
		}

		public void Start<TStateMachine>(ref TStateMachine stateMachine)
			where TStateMachine : IAsyncStateMachine
		{
			stateMachine.MoveNext();
		}

		public void SetResult(T result)
		{
			if (runner != null)
			{
				runner.SetResult(result);
			}
			else
			{
				this.result = result;
			}
		}

		public void SetException(Exception exception)
		{
			if (runner != null)
			{
				runner.SetException(exception);
			}
			else
			{
				this.exception = exception;
			}
		}

		[ExcludeFromCodeCoverage] // no idea when this is called
		public void SetStateMachine(IAsyncStateMachine stateMachine)
		{
			// when this is even called...
		}

		[ExcludeFromCodeCoverage] // no idea when this is called
		public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
			where TAwaiter : INotifyCompletion
			where TStateMachine : IAsyncStateMachine
		{
			runner ??= new AsyncComputeRunner<TStateMachine, T>(ref stateMachine, AutoObservable.Current);
			awaiter.OnCompleted(runner.MoveNext);
		}

		public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
			where TAwaiter : ICriticalNotifyCompletion
			where TStateMachine : IAsyncStateMachine
		{
			runner ??= new AsyncComputeRunner<TStateMachine, T>(ref stateMachine, AutoObservable.Current);
			awaiter.UnsafeOnCompleted(runner.MoveNext);
		}
	}


}