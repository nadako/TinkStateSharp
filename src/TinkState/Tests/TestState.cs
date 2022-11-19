using System.Collections.Generic;
using NUnit.Framework;
using TinkState;

namespace Test
{
	class TestState : BaseTest
	{
		[Test]
		public void TestBasic()
		{
			var state = Observable.State(10);
			Assert.That(state.Value, Is.EqualTo(10));

			state.Value = 20;
			Assert.That(state.Value, Is.EqualTo(20));

			state.Value = 20;
			Assert.That(state.Value, Is.EqualTo(20));

			var bindingCalls = 0;
			var expectedValue = 20;
			void BindCallback(int value)
			{
				bindingCalls++;
				Assert.That(value, Is.EqualTo(expectedValue));
			}

			var binding = state.Bind(BindCallback);
			Assert.That(bindingCalls, Is.EqualTo(1));

			// same value shouldn't trigger bindings
			state.Value = 20;
			Assert.That(bindingCalls, Is.EqualTo(1));

			state.Value = expectedValue = 42;
			Assert.That(bindingCalls, Is.EqualTo(2));

			binding.Dispose();
			state.Value = 100;

			// binding shouldn't be called after being disposed
			Assert.That(bindingCalls, Is.EqualTo(2));
		}

		[Test]
		public void TestWithComparer()
		{
			var state = Observable.State("world", new CustomComparer());
			Assert.That(state.Value, Is.EqualTo("world"));

			var bindingCalls = 0;
			var expectedValue = "world";
			void BindCallback(string value)
			{
				bindingCalls++;
				Assert.That(value, Is.EqualTo(expectedValue));
			}

			state.Bind(BindCallback);
			Assert.That(bindingCalls, Is.EqualTo(1));

			// value considered the same, so it's not changed and binding is not invoked
			state.Value = expectedValue = "WORLD";
			Assert.That(state.Value, Is.EqualTo("world"));
			Assert.That(bindingCalls, Is.EqualTo(1));

			state.Value = expectedValue = "Planet";
			Assert.That(state.Value, Is.EqualTo("Planet"));
			Assert.That(bindingCalls, Is.EqualTo(2));
		}
	}

	class CustomComparer : IEqualityComparer<string>
	{
		public bool Equals(string x, string y)
		{
			if (x == null && y == null) return true;
			if (x == null || y == null) return false;
			return x.ToLower() == y.ToLower();
		}

		public int GetHashCode(string obj)
		{
			return obj.ToLower().GetHashCode();
		}
	}
}