using System;
using System.Collections.Generic;
using UnityEngine;

namespace TinkState
{
	/// <summary>
	/// Run-time component for Unity game objects to manage <see cref="IDisposable"/> objects added with <see cref="DisposableLifetimeHelperExt"/> methods.
	/// </summary>
	[DisallowMultipleComponent]
	class DisposableLifetimeHelper : MonoBehaviour
	{
		struct OnActiveRun
		{
			public Func<IDisposable> Run;
			public IDisposable Current;
		}

		List<IDisposable> disposables;
		List<OnActiveRun> onActiveRuns;

		public void RunOnActive(Func<IDisposable> run)
		{
			if (onActiveRuns == null) onActiveRuns = new List<OnActiveRun>(1);

			var current = gameObject.activeInHierarchy ? run() : null;
			onActiveRuns.Add(new OnActiveRun { Run = run, Current = current });
		}

		public void ClearOnActiveRuns()
		{
			if (onActiveRuns == null) return;

			foreach (var entry in onActiveRuns)
			{
				entry.Current?.Dispose();
			}
			onActiveRuns.Clear();
		}

		public void DisposeOnDestroy(IDisposable disposable)
		{
			disposables ??= new List<IDisposable>(1);
			disposables.Add(disposable);
		}

		void OnEnable()
		{
			if (onActiveRuns == null) return;

			var currentCount = onActiveRuns.Count;
			for (var i = 0; i < currentCount; i++)
			{
				var run = onActiveRuns[i].Run;
				var current = run();
				onActiveRuns[i] = new OnActiveRun {Run = run, Current = current};
			}
		}

		void OnDisable()
		{
			if (onActiveRuns == null) return;

			foreach (var entry in onActiveRuns)
			{
				entry.Current?.Dispose();
			}
		}

		void OnDestroy()
		{
			if (disposables == null) return;
			foreach (var disposable in disposables) disposable.Dispose();
			disposables = null;
		}
	}

	/// <summary>
	/// Static extensions for attaching <see cref="IDisposable"/> lifetime to Unity game objects.
	/// </summary>
	public static class DisposableLifetimeHelperExt
	{
		// TODO: have better names for these...
		/// <summary>
		/// Run given function when the game object becomes active. If the game object is already active,
		/// the function will be run immediately. The returned <see cref="IDisposable"/> will be automatically
		/// disposed when the game object becomes inactive (or destroyed).
		/// </summary>
		/// <remarks>
		/// This can be used to automatically bind and unbind from <see cref="Observable{T}"/> states for
		/// game objects that are designed to be activated and deactivated multiple times (e.g. when pooling).
		/// </remarks>
		/// <param name="gameObject">Game object to attach to</param>
		/// <param name="run">Function to call on game object activation</param>
		public static void RunOnActive(this GameObject gameObject, Func<IDisposable> run)
		{
			if (gameObject == null) return; // already destroyed
			GetHelper(gameObject).RunOnActive(run);
		}

		// TODO: add an overload for function that returns multiple IDisposables? or maybe add CompositeDisposable

		/// <summary>
		/// Dispose and clear any functions previously registered with <see cref="RunOnActive"/> for this game object.
		/// </summary>
		/// <remarks>
		/// This is useful for pooled game objects. Call this before returning the object to pool to dispose its current bindings.
		/// </remarks>
		/// <param name="gameObject">Game object to dispose runs for</param>
		public static void ClearOnActiveRuns(this GameObject gameObject)
		{
			if (gameObject == null) return; // already destroyed
			GetHelper(gameObject).ClearOnActiveRuns();
		}

		/// <summary>
		/// Dispose given <paramref name="disposable"/> when the game object is destroyed.
		/// If the game object is already destroyed, <paramref name="disposable"/> will be disposed immediately.
		/// </summary>
		/// <param name="gameObject">Game object to attach to</param>
		/// <param name="disposable">Disposable to attach</param>
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