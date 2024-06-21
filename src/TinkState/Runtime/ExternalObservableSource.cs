using System;

namespace TinkState
{
	public interface ExternalObservableSource<out T> : Observable<T>
	{
		event Action Subscribed;
		event Action Unsubscribed;
		void Invalidate();
	}
}
