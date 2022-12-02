using System;

namespace TinkState
{
	/// <summary>
	/// Enumeration of possible <see cref="AsyncComputeResult{T}.Status"/> values in <see cref="AsyncComputeResult{T}"/>
	/// </summary>
	public enum AsyncComputeStatus
	{
		/// <summary>
		/// The result is not yet computed.
		/// </summary>
		Loading,
		/// <summary>
		/// The result has computed successfully.
		/// </summary>
		Done,
		/// <summary>
		/// An error has happened during the computation.
		/// </summary>
		Failed,
	}

	/// <summary>
	/// Structure representing the result of an <see cref="Observable.Auto{T}(System.Func{AsyncComputeTask{T}})">asynchronous auto-observable</see> computation.
	/// </summary>
	/// <remarks>
	/// The actual structure depends on the <see cref="Status"/> field:
	/// <list>
	///	<item><see cref="AsyncComputeStatus.Loading"/> meaning that the result is not yet computed and none of other fields are initialized.</item>
	/// <item><see cref="AsyncComputeStatus.Done"/> meaning that the result is computed and is available in the <see cref="Result"/> field.</item>
	/// <item><see cref="AsyncComputeStatus.Failed"/> meaning that the computation has failed and the exception is set in the <see cref="Exception"/> field.</item>
	/// </list>
	/// </remarks>
	/// <typeparam name="T">Type of the value returned by the computation.</typeparam>
	public readonly struct AsyncComputeResult<T>
	{
		/// <summary>
		/// Current status of the value computation.
		/// </summary>
		public readonly AsyncComputeStatus Status;

		/// <summary>
		/// Computed result. Only available when <see cref="Status"/> is <see cref="AsyncComputeStatus.Done"/>.
		/// </summary>
		public readonly T Result;

		/// <summary>
		/// Exception representing an error that happened during computation. Only available when <see cref="Status"/> is <see cref="AsyncComputeStatus.Failed"/>.
		/// </summary>
		public readonly Exception Exception;

		AsyncComputeResult(AsyncComputeStatus status, T result, Exception exception)
		{
			Status = status;
			Result = result;
			Exception = exception;
		}

		public AsyncComputeResult<TOut> Map<TOut>(Func<T, TOut> transform)
		{
			return Status switch
			{
				AsyncComputeStatus.Done => AsyncComputeResult<TOut>.CreateDone(transform(Result)),
				AsyncComputeStatus.Failed => AsyncComputeResult<TOut>.CreateFailed(Exception),
				_ => new AsyncComputeResult<TOut>(Status, default, null),
			};
		}

		public static AsyncComputeResult<T> CreateLoading()
		{
			return new AsyncComputeResult<T>(AsyncComputeStatus.Loading, default, null);
		}

		public static AsyncComputeResult<T> CreateDone(T result)
		{
			return new AsyncComputeResult<T>(AsyncComputeStatus.Done, result, null);
		}

		public static AsyncComputeResult<T> CreateFailed(Exception exception)
		{
			return new AsyncComputeResult<T>(AsyncComputeStatus.Failed, default, exception);
		}
	}
}