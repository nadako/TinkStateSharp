using System;
using System.Collections.Generic;
using UnityEngine;

// TODO: this should probably go to TinkState-Unity
static class DisposableHelper
{
	public static void DisposeOnDestroy(this MonoBehaviour behaviour, IDisposable disposable)
	{
		// TODO: use destroyCancellationToken for Unity 2022
		behaviour.gameObject.DisposeOnDestroy(disposable);
	}

	public static void DisposeOnDestroy(this GameObject go, IDisposable disposable)
	{
		// TODO: use destroyCancellationToken for Unity 2022
		if (!go.TryGetComponent<OnDestroyTrigger>(out var component))
		{
			component = go.AddComponent<OnDestroyTrigger>();
		}
		component.Add(disposable);
	}
}

[DisallowMultipleComponent]
public sealed class OnDestroyTrigger : MonoBehaviour
{
	List<IDisposable> disposables = new List<IDisposable>();

	public void Add(IDisposable disposable)
	{
		disposables.Add(disposable);
	}

	void OnDestroy()
	{
		foreach (var disposable in disposables)
		{
			disposable.Dispose();
		}
		disposables = null;
	}
}
