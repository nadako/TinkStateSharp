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

		// shouldn't fail
		go.ClearOnDestroyDisposes();

		// still 1
		Assert.That(dispose1Calls, Is.EqualTo(1));
		Assert.That(dispose2Calls, Is.EqualTo(1));
	}

	[Test]
	public void TestRunOnActive()
	{
		var go = new GameObject();
		var runs = 0;
		var disposes = 0;
		go.RunOnActive(() =>
		{
			runs++;
			return new CallbackDisposable(() => disposes++);
		});

		Assert.That(runs, Is.EqualTo(1));
		Assert.That(disposes, Is.EqualTo(0));

		go.SetActive(false);
		Assert.That(runs, Is.EqualTo(1));
		Assert.That(disposes, Is.EqualTo(1));

		go.SetActive(true);
		Assert.That(runs, Is.EqualTo(2));
		Assert.That(disposes, Is.EqualTo(1));

		go.SetActive(false);
		Assert.That(runs, Is.EqualTo(2));
		Assert.That(disposes, Is.EqualTo(2));
	}

	[Test]
	public void TestRunOnActiveWhenInactive()
	{
		var go = new GameObject();
		go.SetActive(false);
		var runs = 0;
		var disposes = 0;
		go.RunOnActive(() =>
		{
			runs++;
			return new CallbackDisposable(() => disposes++);
		});

		Assert.That(runs, Is.EqualTo(0));
		Assert.That(disposes, Is.EqualTo(0));

		go.SetActive(true);
		go.SetActive(false);
		Assert.That(runs, Is.EqualTo(1));
		Assert.That(disposes, Is.EqualTo(1));
	}

	[Test]
	public void TestClearOnActiveRuns()
	{
		var go = new GameObject();
		var runs1 = 0;
		var runs2 = 0;
		var disposes1 = 0;
		var disposes2 = 0;
		go.RunOnActive(() =>
		{
			runs1++;
			return new CallbackDisposable(() => disposes1++);
		});
		go.RunOnActive(() =>
		{
			runs2++;
			return new CallbackDisposable(() => disposes2++);
		});

		Assert.That(runs1, Is.EqualTo(1));
		Assert.That(disposes1, Is.EqualTo(0));
		Assert.That(runs2, Is.EqualTo(1));
		Assert.That(disposes2, Is.EqualTo(0));

		go.ClearOnActiveRuns();
		Assert.That(runs1, Is.EqualTo(1));
		Assert.That(disposes1, Is.EqualTo(1));
		Assert.That(runs2, Is.EqualTo(1));
		Assert.That(disposes2, Is.EqualTo(1));

		go.ClearOnActiveRuns();
		go.SetActive(true);
		go.SetActive(false);
		Assert.That(runs1, Is.EqualTo(1));
		Assert.That(disposes1, Is.EqualTo(1));
		Assert.That(runs2, Is.EqualTo(1));
		Assert.That(disposes2, Is.EqualTo(1));
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