using System.Collections.Generic;

namespace TinkState.Internal
{
	interface Observer
	{
		void Notify();
	}

	class Dispatcher : Dispatcher.Interface
	{
		internal interface Interface
		{
			bool CanFire();
			void Subscribe(Observer observer);
			void Unsubscribe(Observer observer);
		}

		protected long revision;
		List<Observer> observers; // TODO: better structure?

		protected Dispatcher()
		{
			revision = Revision.New();
			observers = new List<Observer>();
		}

		bool Interface.CanFire()
		{
			return observers != null;
		}

		void Interface.Subscribe(Observer observer)
		{
			if (observers.Contains(observer)) return;
			var wasEmpty = observers.Count == 0;
			observers.Add(observer);
			if (wasEmpty) OnStatusChange(true);
		}

		void Interface.Unsubscribe(Observer observer)
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