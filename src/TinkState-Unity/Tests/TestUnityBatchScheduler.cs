using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using TinkState;

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
}
