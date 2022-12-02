using NUnit.Framework;
using TinkState;

namespace Test
{
	class TestAuto : BaseTest
	{
		[Test]
		public void TestBasic()
		{
			var s1 = Observable.State(1);
			var s2 = Observable.State(2);

			var computeCalls = 0;
			var o = Observable.Auto(() =>
			{
				computeCalls++;
				return s1.Value + s2.Value;
			});

			Assert.That(computeCalls, Is.Zero);

			Assert.That(o.Value, Is.EqualTo(3));
			Assert.That(computeCalls, Is.EqualTo(1));
			// second value access must not call compute again
			Assert.That(o.Value, Is.EqualTo(3));
			Assert.That(computeCalls, Is.EqualTo(1));

			s1.Value = 2;
			s2.Value = 3;

			// simply changing source data shouldn't invoke computation
			Assert.That(computeCalls, Is.EqualTo(1));

			// but actually accessing value should trigger recomputation
			Assert.That(o.Value, Is.EqualTo(5));
			Assert.That(computeCalls, Is.EqualTo(2));
			// second value access must not call compute again
			Assert.That(o.Value, Is.EqualTo(5));
			Assert.That(computeCalls, Is.EqualTo(2));
		}

		[Test]
		public void TestNestedAndBinding()
		{
			var s1 = Observable.State(1);
			var s2 = Observable.State(2);
			var s3 = Observable.State(3);
			var c = Observable.Const(10);

			var computeCalls1 = 0;
			var o1 = Observable.Auto(() =>
			{
				computeCalls1++;
				return s1.Value + s2.Value;
			});

			var computeCalls2 = 0;
			var o2 = Observable.Auto(() =>
			{
				computeCalls2++;
				return o1.Value + s3.Value + c.Value;
			});

			Assert.That(computeCalls1, Is.Zero);
			Assert.That(computeCalls2, Is.Zero);

			Assert.That(o2.Value, Is.EqualTo(16));
			Assert.That(computeCalls1, Is.EqualTo(1));
			Assert.That(computeCalls2, Is.EqualTo(1));

			// second access shouldn't recalculate anything
			Assert.That(o2.Value, Is.EqualTo(16));
			Assert.That(computeCalls1, Is.EqualTo(1));
			Assert.That(computeCalls2, Is.EqualTo(1));

			s3.Value = 4;
			Assert.That(o2.Value, Is.EqualTo(17));
			// o1 hasn't changed as it doesn't depend on s3, so it shouldn't be recalculated
			Assert.That(computeCalls1, Is.EqualTo(1));
			Assert.That(computeCalls2, Is.EqualTo(2));

			// let's also test bindings in this rather complex scenario for better coverage
			var bindingCalls = 0;
			var expectedValue = 17;
			var binding = o2.Bind(value =>
			{
				bindingCalls++;
				Assert.That(value, Is.EqualTo(expectedValue));
			});

			Assert.That(bindingCalls, Is.EqualTo(1));
			// no recomputations are needed for binding since the value hasn't changed
			Assert.That(computeCalls1, Is.EqualTo(1));
			Assert.That(computeCalls2, Is.EqualTo(2));

			expectedValue = 18;
			s1.Value = 2;
			Assert.That(bindingCalls, Is.EqualTo(2));
			Assert.That(computeCalls1, Is.EqualTo(2));
			Assert.That(computeCalls2, Is.EqualTo(3));

			// if we dispose a last binding, not only the callback shouldn't be invoked,
			// but also computations shouldn't trigger if we change source data...
			binding.Dispose();
			s1.Value = 3;
			Assert.That(bindingCalls, Is.EqualTo(2));
			Assert.That(computeCalls1, Is.EqualTo(2));
			Assert.That(computeCalls2, Is.EqualTo(3));

			// ...unless we read values again
			Assert.That(o2.Value, Is.EqualTo(19));
			Assert.That(computeCalls1, Is.EqualTo(3));
			Assert.That(computeCalls2, Is.EqualTo(4));
		}

		[Test]
		public void TestSubscriptions()
		{
			var sb = Observable.State(true);
			var s1 = Observable.State("foo");
			var s2 = Observable.State("bar");
			var s3 = Observable.State("!");

			// this auto-observable should subscribe and unsubscribe properly depending on the condition
			var o = Observable.Auto(() => (sb.Value ? s1.Value : s2.Value) + s3.Value);

			var expectedValue = "foo!";
			var bindingCalls = 0;
			var binding = o.Bind(value =>
			{
				bindingCalls++;
				Assert.That(value, Is.EqualTo(expectedValue));
			});
			Assert.That(bindingCalls, Is.EqualTo(1));

			expectedValue = "baz!";
			s1.Value = "baz";
			Assert.That(bindingCalls, Is.EqualTo(2));

			s2.Value = "qux";
			// binding not invoked since we don't care about s2 yet
			Assert.That(bindingCalls, Is.EqualTo(2));

			expectedValue = "qux!";
			sb.Value = false;
			Assert.That(bindingCalls, Is.EqualTo(3));

			s1.Value = "thud";
			// binding not invoked since we don't care about s1 anymore
			Assert.That(bindingCalls, Is.EqualTo(3));

			expectedValue = "thud!";
			sb.Value = true;
			Assert.That(bindingCalls, Is.EqualTo(4));
		}

		[Test]
		public void TestNoSubscriptions()
		{
			var c = Observable.Const(41);
			var o = Observable.Auto(() => c.Value + 1);
			Assert.That(o.Value, Is.EqualTo(42));
			// TODO: check internals that it can't fire?

			var bindingCalls = 0;
			var binding = o.Bind(value =>
			{
				bindingCalls++;
				Assert.That(value, Is.EqualTo(42));
			});
			Assert.That(bindingCalls, Is.EqualTo(1));
			Assert.DoesNotThrow(() => binding.Dispose());
		}

		[Test]
		public void TestNoSubscriptionsAnymore()
		{
			// this example is rather artificial, as the computation code shouldn't depend on non-observable mutable values,
			// but who knows, maybe someone will come up with some use-case for that. the test here basically covers the case
			// where a binding should be disposed if its observable will never fire anymore after the last recomputation
			var s = Observable.State(10);
			var track = true;
			var o = Observable.Auto(() =>
			{
				if (track) return s.Value + 1;
				else return 42;
			});

			var bindingCalls = 0;
			var expectedValue = 11;
			o.Bind(value =>
			{
				bindingCalls++;
				Assert.That(value, Is.EqualTo(expectedValue));
			});
			Assert.That(bindingCalls, Is.EqualTo(1));

			track = false;
			expectedValue = 42;
			s.Value++;
			Assert.That(bindingCalls, Is.EqualTo(2));

			s.Value++;
			Assert.That(bindingCalls, Is.EqualTo(2));
		}

		[Test]
		public void TestDontRecomputeIfSubsUpdatedButNotChanged()
		{
			// test an edge case where a source observable got a new revision
			// but has the same value as before by the time we access the derived
			// auto-observable, in which case we check if source has actually changed,
			// and if not - skip recomputation for the auto-observable
			var s = Observable.State(10);
			var computeCalls = 0;
			var o = Observable.Auto(() =>
			{
				computeCalls++;
				return s.Value;
			});
			Assert.That(o.Value, Is.EqualTo(10));
			Assert.That(computeCalls, Is.EqualTo(1));
			s.Value = 15;
			s.Value = 10;
			Assert.That(o.Value, Is.EqualTo(10));
			Assert.That(computeCalls, Is.EqualTo(1));
		}

		[Test]
		public void TestSameSourceUsedTwice()
		{
			// another edge case where subscription is processed twice
			// but we only want to call reuse logic once to save some work

			var s = Observable.State(10);
			var o = Observable.Auto(() => s.Value + s.Value);

			// on first computation the subscription is initialized
			Assert.That(o.Value, Is.EqualTo(20));

			s.Value = 20;

			// on next recalculation it should be reused on first access
			// and simply return value on subsequent one(s)
			Assert.That(o.Value, Is.EqualTo(40));
		}

		[Test]
		public void TestChangesWhileComputing()
		{
			// a compute function that changes source observables...
			// this is a bad use case, but it can happen and we handle it, so test it

			var s1 = Observable.State(0);
			var s2 = Observable.State(0);
			var s3 = Observable.State(0);
			var computeCalls = 0;
			var o = Observable.Auto(() =>
			{
				computeCalls++;
				if (s1.Value < 10) s1.Value++;
				if (s2.Value < 10) s2.Value++;
				return s1.Value + s2.Value + s3.Value;
			});

			Assert.That(s1.Value, Is.Zero);
			Assert.That(s2.Value, Is.Zero);
			Assert.That(o.Value, Is.EqualTo(20));
			Assert.That(s1.Value, Is.EqualTo(10));
			Assert.That(s2.Value, Is.EqualTo(10));

			// compute is run 11 times...
			Assert.That(computeCalls, Is.EqualTo(11));

			computeCalls = 0;

			s1.Value = s2.Value = 0;
			s3.Value = 1;

			var bindingCalls = 0;
			var expectedValue = 21;
			o.Bind(value =>
			{
				bindingCalls++;
				Assert.That(value, Is.EqualTo(expectedValue));
			});
			Assert.That(bindingCalls, Is.EqualTo(1));

			Assert.That(computeCalls, Is.EqualTo(11));

			expectedValue = 23;
			s3.Value = 3;
			Assert.That(bindingCalls, Is.EqualTo(2));
			Assert.That(computeCalls, Is.EqualTo(12));
		}

		[Test]
		public void TestAutoRun()
		{
			var s1 = Observable.State(1);
			var s2 = Observable.State(2);

			var expectedValue = 3;
			var actionCalls = 0;

			var binding = Observable.AutoRun(() =>
			{
				actionCalls++;
				Assert.That(s1.Value + s2.Value, Is.EqualTo(expectedValue));
			});
			Assert.That(actionCalls, Is.EqualTo(1));

			expectedValue = 5;
			s1.Value = 3;
			Assert.That(actionCalls, Is.EqualTo(2));

			expectedValue = 6;
			s2.Value = 3;
			Assert.That(actionCalls, Is.EqualTo(3));

			binding.Dispose();

			s1.Value = 100;
			s2.Value = 500;
			Assert.That(actionCalls, Is.EqualTo(3));
		}

		[Test]
		public void TestMapConst()
		{
			var c = Observable.Const(10);

			var computeCalls = 0;
			var o = Observable.Auto(() =>
			{
				computeCalls++;
				return c.Value * 2;
			});

			// TODO: also test not accessing value before mapping, when TODO in CanFire is addressed
			Assert.That(o.Value, Is.EqualTo(20));
			Assert.That(computeCalls, Is.EqualTo(1));

			var transformCalls = 0;
			var m = o.Map(value =>
			{
				transformCalls++;
				return (value * 2).ToString();
			});

			Assert.That(computeCalls, Is.EqualTo(1));
			Assert.That(transformCalls, Is.Zero);

			Assert.That(m.Value, Is.EqualTo("40"));
			Assert.That(computeCalls, Is.EqualTo(1));
			Assert.That(transformCalls, Is.EqualTo(1));

			Assert.That(m.Value, Is.EqualTo("40"));
			Assert.That(computeCalls, Is.EqualTo(1));
			Assert.That(transformCalls, Is.EqualTo(1));

			// TODO: test map from map and bind
		}

		[Test]
		public void TestMapNonConst()
		{
			var s = Observable.State(10);

			var computeCalls = 0;
			var o = Observable.Auto(() =>
			{
				computeCalls++;
				return s.Value * 2;
			});

			var transformCalls = 0;
			var m = o.Map(value =>
			{
				transformCalls++;
				return (value * 2).ToString();
			});

			Assert.That(computeCalls, Is.Zero);
			Assert.That(transformCalls, Is.Zero);

			Assert.That(m.Value, Is.EqualTo("40"));
			Assert.That(computeCalls, Is.EqualTo(1));
			Assert.That(transformCalls, Is.EqualTo(1));

			Assert.That(m.Value, Is.EqualTo("40"));
			Assert.That(computeCalls, Is.EqualTo(1));
			Assert.That(transformCalls, Is.EqualTo(1));

			s.Value = 20;

			Assert.That(m.Value, Is.EqualTo("80"));
			Assert.That(computeCalls, Is.EqualTo(2));
			Assert.That(transformCalls, Is.EqualTo(2));

			// TODO: test map from map and bind
		}
	}
}
