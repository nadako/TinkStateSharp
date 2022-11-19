using System.Runtime.CompilerServices;
using System;
using TinkState.Internal;

namespace TinkState
{
	/// <summary>
	/// Task-like type representing <see cref="Observable.Auto{T}(System.Func{AsyncComputeTask{T}})">asynchronous auto-observable computations</see>.
	/// </summary>
	/// <remarks>
	/// This must be the return type for <c>async</c> computation functions as it enables proper observable tracking at each <c>async</c> function
	/// execution step.
	/// </remarks>
	/// <typeparam name="T">Type of the computed value.</typeparam>
	[AsyncMethodBuilder(typeof(AsyncComputeTaskBuilder<>))]
	public readonly struct AsyncComputeTask<T>
	{
		readonly AsyncComputeTaskSource<T> source;
		readonly AsyncComputeResult<T> result;

		internal AsyncComputeTask(AsyncComputeTaskSource<T> source)
		{
			this.source = source;
			this.result = default;
		}

		AsyncComputeTask(AsyncComputeResult<T> result)
		{
			this.source = null;
			this.result = result;
		}

		internal static AsyncComputeTask<T> FromException(Exception exception)
		{
			return new AsyncComputeTask<T>(new AsyncComputeResult<T>(AsyncComputeStatus.Failed, default, exception));
		}

		internal static AsyncComputeTask<T> FromResult(T result)
		{
			return new AsyncComputeTask<T>(new AsyncComputeResult<T>(AsyncComputeStatus.Done, result, null));
		}

		internal AsyncComputeResult<T> GetResult()
		{
			return (source != null) ? source.GetResult() : result;
		}

		internal void OnComplete(Action callback)
		{
			if (source == null)
			{
				// this should never really be the case as we ever only call this method when the computation
				// is suspended due to await, otherwise the result would be set synchronously
				callback();
			}
			else
			{
				source.OnComplete(callback);
			}
		}

		internal void CancelOnComplete()
		{
			if (source != null) source.CancelOnComplete();
		}
	}
}