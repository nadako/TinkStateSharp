using System;

namespace TinkState
{
	public interface Stream<T> // TODO: split stream read and stream dispatch
	{
		IDisposable Bind(Action<T> callback, Scheduler scheduler = null);
		void Dispatch(T data);
	}
}