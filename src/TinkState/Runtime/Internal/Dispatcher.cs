using System.Collections.Generic;

namespace TinkState.Internal
{
	interface Observer
	{
		void Notify();
	}

	class Dispatcher
	{
		protected long revision;
		List<Observer> observers; // TODO: better structure?

		protected Dispatcher()
		{
			revision = Revision.New();
			observers = new List<Observer>();
		}

		public bool CanFire()
		{
			// TODO: in AutoObservable try computing before the check so we dispose if there's no dispatching subscriptions
			return observers != null;
		}

		public void Subscribe(Observer observer)
		{
			if (observers.Contains(observer)) return;
			var wasEmpty = observers.Count == 0;
			observers.Add(observer);
			if (wasEmpty) OnStatusChange(true);
		}

		public void Unsubscribe(Observer observer)
		{
			if (observers == null) return; // a binding can try to unsubscribe when an observable becomes disposed, do nothing in this case

			var wasNotEmpty = observers.Count > 0;
			observers.Remove(observer);
			if (wasNotEmpty && observers.Count == 0) OnStatusChange(false);
		}

		protected virtual void OnStatusChange(bool active) { }

		protected void Fire()
		{
			revision = Revision.New();
			foreach (var observer in observers.ToArray()) // TODO: get rid of this copying and deal with remove-synchronously-while-iterating somehow
			{
				observer.Notify();
			}
		}

		protected void Dispose()
		{
			observers = null;
		}
	}
}