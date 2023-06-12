using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using TinkState;
using UnityEngine;

public class TestUnityBatchScheduler
{
    [UnityTest]
    public IEnumerator TestBindingDelayed()
    {
        var state = Observable.State(10);
        var bindingCalls = 0;
        var expectedValue = 10;
        state.Bind(value =>
        {
            bindingCalls++;
            Assert.That(value, Is.EqualTo(expectedValue));
        });
        Assert.That(bindingCalls, Is.EqualTo(1));
        state.Value = 15;
        Assert.That(bindingCalls, Is.EqualTo(1));
        expectedValue = 15;
        yield return null;
        Assert.That(bindingCalls, Is.EqualTo(2));
    }

    [UnityTest]
    public IEnumerator TestBindingInvokedOnceForMultipleChanges()
    {
        var state = Observable.State(10);
        var bindingCalls = 0;
        var expectedValue = 10;
        state.Bind(value =>
        {
            bindingCalls++;
            Assert.That(value, Is.EqualTo(expectedValue));
        });
        Assert.That(bindingCalls, Is.EqualTo(1));
        state.Value = 15;
        state.Value = 50;
        state.Value = 5;
        Assert.That(bindingCalls, Is.EqualTo(1));
        expectedValue = 5;
        yield return null;
        Assert.That(bindingCalls, Is.EqualTo(2));
    }

    [UnityTest]
    public IEnumerator TestBindingNotInvokedIfFinalValueIsSame()
    {
        var state = Observable.State(10);
        var bindingCalls = 0;
        var expectedValue = 10;
        state.Bind(value =>
        {
            bindingCalls++;
            Assert.That(value, Is.EqualTo(expectedValue));
        });
        Assert.That(bindingCalls, Is.EqualTo(1));
        state.Value = 15;
        state.Value = 50;
        state.Value = 10;
        Assert.That(bindingCalls, Is.EqualTo(1));
        yield return null;
        Assert.That(bindingCalls, Is.EqualTo(1));
    }

    // NOTE: this is not an intended usage for bindings, as one shouldn't
    // trigger bindings from other bindings (e.g. by changing a State) and
    // instead make use of auto-Observables and such, but we still test
    // that this works as expected and doesn't hang
    [UnityTest]
    public IEnumerator TestTriggerBindingFromBinding()
    {
	    var s = Observable.State(2);
	    var s2 = Observable.State(2);

	    var bindingCalls = 0;
	    var expectedValue = 2;
	    void BindCallback(int value)
	    {
		    bindingCalls++;
		    Assert.That(value, Is.EqualTo(expectedValue));
	    }

	    s2.Bind(BindCallback);
	    Assert.That(bindingCalls, Is.EqualTo(1));

	    s.Bind(v => s2.Value = v * 2);

	    // first binding was still not called because it's scheduled!
	    Assert.That(bindingCalls, Is.EqualTo(1));

	    // it will be called as scheduled though
	    expectedValue = 4;
	    yield return null;
	    Assert.That(bindingCalls, Is.EqualTo(2));

	    // changing the value will invoke the binding for `s` which will in its turn schedule the binding for `s2`,
	    // which should be processed right after, let's check that
	    s.Value = 3;
	    expectedValue = 6;
	    yield return null;
	    Assert.That(bindingCalls, Is.EqualTo(3));
    }
}