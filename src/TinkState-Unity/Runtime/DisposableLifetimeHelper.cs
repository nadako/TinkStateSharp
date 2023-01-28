using System;
using System.Collections.Generic;
using UnityEngine;

namespace TinkState
{
	[DisallowMultipleComponent]
	public class DisposableLifetimeHelper : MonoBehaviour
	{
		public delegate IDisposable WhenEnabledDelegate();

		struct WhenEnabled
		{
			public WhenEnabledDelegate wakeup;
			public IDisposable current;
		}

		List<IDisposable> disposables;
		List<WhenEnabled> whenEnabled;

		public void RunWhenEnabled(WhenEnabledDelegate wakeup)
		{
			if (whenEnabled == null) whenEnabled = new List<WhenEnabled>(1);

			var current = gameObject.activeInHierarchy ? wakeup() : null;
			whenEnabled.Add(new WhenEnabled { wakeup = wakeup, current = current });
		}

		public void ClearWhenEnabledRuns()
		{
			if (whenEnabled == null) return;

			foreach (var entry in whenEnabled)
			{
				entry.current?.Dispose();
			}
			whenEnabled.Clear();
		}

		public void DisposeOnDestroy(IDisposable disposable)
		{
			disposables ??= new List<IDisposable>(1);
			disposables.Add(disposable);
		}

		void OnEnable()
		{
			if (whenEnabled == null) return;

			var currentCount = whenEnabled.Count;
			for (var i = 0; i < currentCount; i++)
			{
				var wakeup = whenEnabled[i].wakeup;
				var current = wakeup();
				whenEnabled[i] = new WhenEnabled {wakeup = wakeup, current = current};
			}
		}

		void OnDisable()
		{
			if (whenEnabled == null) return;

			foreach (var entry in whenEnabled)
			{
				entry.current?.Dispose();
			}
		}

		void OnDestroy()
		{
			if (disposables == null) return;
			foreach (var disposable in disposables) disposable.Dispose();
			disposables = null;
		}
	}

	public static class DisposableLifetimeHelperExt
	{
		// TODO: have better names for these...
		public static void ClearWhenEnabledRuns(this GameObject gameObject)
		{
			if (gameObject == null) return; // already destroyed
			GetHelper(gameObject).ClearWhenEnabledRuns();
		}

		public static void RunWhenEnabled(this GameObject gameObject, DisposableLifetimeHelper.WhenEnabledDelegate wakeup)
		{
			if (gameObject == null) return; // already destroyed
			GetHelper(gameObject).RunWhenEnabled(wakeup);
		}

		public static void DisposeOnDestroy(this GameObject gameObject, IDisposable disposable)
		{
			if (gameObject == null) // already destroyed
			{
				disposable.Dispose();
				return;
			}
			GetHelper(gameObject).DisposeOnDestroy(disposable);
		}

		static DisposableLifetimeHelper GetHelper(GameObject gameObject)
		{
			var helper = gameObject.GetComponent<DisposableLifetimeHelper>();
			return helper ?? gameObject.AddComponent<DisposableLifetimeHelper>();
		}
	}
}