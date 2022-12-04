using NUnit.Framework;
using System;
using TinkState;

namespace Test
{

	// here we test some rare-ish edge cases when a dispatching observable can be subscribed to/unsubscribed from
	// as a result of directly invoked notification
	class TestModifyWhileFiring : BaseTest
	{
		[Test]
		public void TestAdding()
		{
			var state = Observable.State(1);
			var innerBindingCalls = 0;
			IDisposable binding = null;
			IDisposable innerBinding = null;
			binding = state.Bind(_ =>
			{
				if (binding == null) return; // do nothing on the initial invocation
				if (innerBinding == null)
				{
					innerBinding = state.Bind(_ => { innerBindingCalls++; });
				}
			});

			Assert.That(innerBindingCalls, Is.Zero);

			state.Value = 2;
			Assert.That(innerBindingCalls, Is.EqualTo(1)); // should be only called once on initial binding
			state.Value = 3;
			Assert.That(innerBindingCalls, Is.EqualTo(2));
		}

		[Test]
		public void TestRemoving()
		{
			var state = Observable.State(1);

			IDisposable binding = null;
			int disposeCalls = 0;
			binding = state.Bind(_ =>
			{
				if (binding == null) return; // do nothing on the initial invocation
				binding.Dispose();
				disposeCalls++;
			});

			Assert.That(disposeCalls, Is.Zero);
			state.Value = 2;
			Assert.That(disposeCalls, Is.EqualTo(1));
			state.Value = 3;
			Assert.That(disposeCalls, Is.EqualTo(1)); // disposed properly
		}

		[Test]
		public void TestConvolutedAuto()
		{
			// one shouldn't do such horrible things, but we still try to handle everything
			var s1 = Observable.State("s1-0");
			var s2 = Observable.State("s2-0");
			var s3 = Observable.State(true);

			var o = Observable.Auto(() => s3.Value ? s1.Value : s2.Value);
			var expectedValue = "s1-0";
			var bindingCalls = 0;
			o.Bind(value =>
			{
				bindingCalls++;
				Assert.That(value, Is.EqualTo(expectedValue));
			});
			Assert.That(bindingCalls, Is.EqualTo(1));

			IDisposable binding = null;
			binding = s1.Bind(_ =>
			{
				if (binding == null) return;
				expectedValue = "s2-0";
				s3.Value = false;
				binding.Dispose();
			});
			Assert.That(bindingCalls, Is.EqualTo(1));

			expectedValue = "s1-1";
			s1.Value = "s1-1";
			Assert.That(bindingCalls, Is.EqualTo(3));
		}
	}
}