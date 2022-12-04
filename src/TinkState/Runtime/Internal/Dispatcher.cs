using System;
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
		List<Observer> observers;
		bool firing;
		List<(Observer, bool remove)> modifications;

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
			if (firing)
			{
				modifications ??= new List<(Observer, bool remove)>(1);
				modifications.Add((observer, false));
			}
			else
			{
				if (AddObserver(observer) && observers.Count == 1)
				{
					OnStatusChange(true);
				}
			}
		}

		public void Unsubscribe(Observer observer)
		{
			if (observers == null) return; // a binding can try to unsubscribe when an observable becomes disposed, do nothing in this case

			if (firing)
			{
				modifications ??= new List<(Observer, bool remove)>(1);
				modifications.Add((observer, true));
			}
			else
			{
				var wasEmpty = observers.Count == 0;
				if (!wasEmpty && RemoveObserver(observer))
				{
					if (observers.Count == 0) OnStatusChange(false);
				}
			}
		}

		protected virtual void OnStatusChange(bool active) { }

		protected void Fire()
		{
			revision = Revision.New();

			if (observers.Count == 0) return;

			firing = true;
			foreach (var observer in observers)
			{
				observer.Notify();
			}
			firing = false;

			if (modifications != null)
			{
				foreach (var (observer, remove) in modifications)
				{
					if (remove) RemoveObserver(observer);
					else AddObserver(observer);
				}
				modifications = null;
				var isNowEmpty = observers.Count == 0;
				if (isNowEmpty) OnStatusChange(false);
			}
		}

		bool AddObserver(Observer observer)
		{
			if (!observers.Contains(observer))
			{
				observers.Add(observer);
				return true;
			}
			return false; // we should probably throw here
		}

		bool RemoveObserver(Observer observer)
		{
			return observers.Remove(observer); // we should probably throw on false
		}

		protected void Dispose()
		{
			observers = null;
			modifications = null;
		}
	}
}