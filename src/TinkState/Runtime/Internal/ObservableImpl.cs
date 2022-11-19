using System.Collections.Generic;

namespace TinkState.Internal
{
	interface ObservableImpl : Dispatcher.Interface
	{
		long GetRevision();
	}

	interface ObservableImpl<T> : ObservableImpl
	{
		T GetValueUntracked(); // TODO: rename this to just GetValue or something
		IEqualityComparer<T> GetComparer();
	}
}