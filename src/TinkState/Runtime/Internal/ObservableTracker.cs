using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Nadako.TinkState.Unity.Editor")]

namespace TinkState.Internal
{
	static class ObservableTracker
	{
		public static bool EnableTracking = false;

		static bool isDirty;

		internal readonly struct TrackingData
		{
			public readonly string Type;
			public readonly string Stack;

			public TrackingData(string type, string stack)
			{
				Type = type;
				Stack = stack;
			}
		}

		static readonly Dictionary<WeakReference<Observer>, TrackingData> tracking = new();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void TrackSubscription(Dispatcher dispatcher, Observer observer)
		{
			if (EnableTracking) DoTrackSubscription(dispatcher, observer);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void UntrackSubscription(Dispatcher dispatcher, Observer observer)
		{
			if (EnableTracking) DoUntrackSubscription(dispatcher, observer);
		}

		public static bool CheckDirty()
		{
			var wasDirty = isDirty;
			isDirty = false;
			return wasDirty;
		}

		public static void IterateEntries(Action<Observer, TrackingData> callback)
		{
			foreach (var (weakRef, trackingData) in tracking)
			{
				if (weakRef.TryGetTarget(out var observer))
				{
					callback(observer, trackingData);
				}
			}
		}

		static void DoTrackSubscription(Dispatcher dispatcher, Observer observer)
		{
			var weakRef = new WeakReference<Observer>(observer);
			tracking[weakRef] = new TrackingData(
				observer.GetType().ToString(),
				new StackTrace(2, true).ToString()
			);
			isDirty = true;
		}

		static void DoUntrackSubscription(Dispatcher dispatcher, Observer observer)
		{
			WeakReference<Observer> weakRef = null;
			foreach (var entry in tracking.Keys)
			{
				if (entry.TryGetTarget(out var target) && target == observer)
				{
					weakRef = entry;
					break;
				}
			}

			if (weakRef != null)
			{
				tracking.Remove(weakRef);
				isDirty = true;
			}
		}
	}
}
