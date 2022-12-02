using System.Collections.Generic;

namespace TinkState.Internal
{
	interface ObservableImpl
	{
		long GetRevision();
		bool CanFire();
		void Subscribe(Observer observer);
		void Unsubscribe(Observer observer);
	}

	interface ObservableImpl<T> : ObservableImpl
	{
		T GetValueUntracked(); // TODO: rename this to just GetValue or something
		IEqualityComparer<T> GetComparer();
	}
}