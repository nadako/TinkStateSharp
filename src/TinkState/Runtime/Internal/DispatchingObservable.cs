using System.Collections.Generic;

namespace TinkState.Internal
{
	interface DispatchingObservable
	{
		long GetRevision();
		bool CanFire();
		void Subscribe(Observer observer);
		void Unsubscribe(Observer observer);
	}

	interface DispatchingObservable<T> : DispatchingObservable, ValueProvider<T>
	{
		IEqualityComparer<T> GetComparer();
	}
}