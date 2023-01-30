using System;
using System.Collections;
using NUnit.Framework;
using TinkState;
using UnityEngine;
using UnityEngine.TestTools;

public class TestLifetimeHelper
{
	[UnityTest]
	public IEnumerator TestDisposeOnDestroy()
	{
		var go = new GameObject();
		var disposeCalls = 0;
		go.DisposeOnDestroy(new CallbackDisposable(() => disposeCalls++));
		Assert.That(disposeCalls, Is.EqualTo(0));
		GameObject.Destroy(go);
		yield return null; // Destroy is delayed, so wait a frame
		Assert.That(disposeCalls, Is.EqualTo(1));

		// if GO is already destroyed - disposable should be disposed immediately
		var moreDisposeCalls = 0;
		go.DisposeOnDestroy(new CallbackDisposable(() => moreDisposeCalls++));
		Assert.That(moreDisposeCalls, Is.EqualTo(1));
	}

	[UnityTest]
	public IEnumerator TestDisposeOnDestroyMultiple()
	{
		var go = new GameObject();
		var dispose1Calls = 0;
		var dispose2Calls = 0;
		go.DisposeOnDestroy(
			new CallbackDisposable(() => dispose1Calls++),
			new CallbackDisposable(() => dispose2Calls++)
		);
		Assert.That(dispose1Calls, Is.EqualTo(0));
		Assert.That(dispose2Calls, Is.EqualTo(0));
		GameObject.Destroy(go);
		yield return null; // Destroy is delayed, so wait a frame
		Assert.That(dispose1Calls, Is.EqualTo(1));
		Assert.That(dispose2Calls, Is.EqualTo(1));

		// if GO is already destroyed - disposable should be disposed immediately
		var moreDispose1Calls = 0;
		var moreDispose2Calls = 0;
		go.DisposeOnDestroy(
			new CallbackDisposable(() => moreDispose1Calls++),
			new CallbackDisposable(() => moreDispose2Calls++)
		);
		Assert.That(moreDispose1Calls, Is.EqualTo(1));
		Assert.That(moreDispose2Calls, Is.EqualTo(1));
	}

	[UnityTest]
	public IEnumerator TestClearOnDestroyDisposes()
	{
		var go = new GameObject();
		var dispose1Calls = 0;
		var dispose2Calls = 0;
		go.DisposeOnDestroy(
			new CallbackDisposable(() => dispose1Calls++),
			new CallbackDisposable(() => dispose2Calls++)
		);
		Assert.That(dispose1Calls, Is.EqualTo(0));
		Assert.That(dispose2Calls, Is.EqualTo(0));
		go.ClearOnDestroyDisposes();
		Assert.That(dispose1Calls, Is.EqualTo(1));
		Assert.That(dispose2Calls, Is.EqualTo(1));

		go.ClearOnDestroyDisposes();
		GameObject.Destroy(go);
		yield return null; // Destroy is delayed, so wait a frame

		// still 1
		Assert.That(dispose1Calls, Is.EqualTo(1));
		Assert.That(dispose2Calls, Is.EqualTo(1));
	}

	class CallbackDisposable : IDisposable
	{
		readonly Action callback;

		public CallbackDisposable(Action callback)
		{
			this.callback = callback;
		}

		public void Dispose()
		{
			callback();
		}
	}
}