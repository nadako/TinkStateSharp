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

		[Test]
		public void TestMap()
		{
			var s = Observable.State(42);
			Assert.That(s.Value, Is.EqualTo(42));

			var transformCalls = 0;
			var m = s.Map(value =>
			{
				transformCalls++;
				return (value * 2).ToString();
			});
			Assert.That(transformCalls, Is.Zero);

			Assert.That(m.Value, Is.EqualTo("84"));
			Assert.That(transformCalls, Is.EqualTo(1));
			Assert.That(m.Value, Is.EqualTo("84"));
			Assert.That(transformCalls, Is.EqualTo(1));

			s.Value = 10;
			Assert.That(transformCalls, Is.EqualTo(1));
			Assert.That(m.Value, Is.EqualTo("20"));
			Assert.That(transformCalls, Is.EqualTo(2));

			var bindingCalls = 0;
			var expectedValue = "20";
			m.Bind(value =>
			{
				bindingCalls++;
				Assert.That(value, Is.EqualTo(expectedValue));
			});
			Assert.That(bindingCalls, Is.EqualTo(1));
			Assert.That(transformCalls, Is.EqualTo(2));

			expectedValue = "30";
			s.Value = 15;
			Assert.That(bindingCalls, Is.EqualTo(2));
			Assert.That(transformCalls, Is.EqualTo(3));

			var transform2Calls = 0;
			var m2 = m.Map(value =>
			{
				transform2Calls++;
				return "foo_" + value;
			});
			Assert.That(transformCalls, Is.EqualTo(3));
			Assert.That(transform2Calls, Is.EqualTo(0));

			var expectedValue2 = "foo_30";
			var binding2Calls = 0;
			var binding = m2.Bind(value =>
			{
				binding2Calls++;
				Assert.That(value, Is.EqualTo(expectedValue2));
			});
			Assert.That(binding2Calls, Is.EqualTo(1));
			Assert.That(transform2Calls, Is.EqualTo(1));

			expectedValue = "100";
			expectedValue2 = "foo_100";
			s.Value = 50;
			Assert.That(bindingCalls, Is.EqualTo(3));
			Assert.That(binding2Calls, Is.EqualTo(2));
			Assert.That(transformCalls, Is.EqualTo(4));
			Assert.That(transform2Calls, Is.EqualTo(2));

			binding.Dispose();
			expectedValue = "50";
			s.Value = 25;
			Assert.That(bindingCalls, Is.EqualTo(4));
			Assert.That(binding2Calls, Is.EqualTo(2));
			Assert.That(transformCalls, Is.EqualTo(5));
			Assert.That(transform2Calls, Is.EqualTo(2));
		}

		[Test]
		public void TestMapCustomComparer()
		{
			var s = Observable.State("foo");

			var transformCalls = 0;
			var m = s.Map(value =>
			{
				transformCalls++;
				return "1_" + value;
			}, new CustomComparer());

			Assert.That(m.Value, Is.EqualTo("1_foo"));
			Assert.That(transformCalls, Is.EqualTo(1));

			var bindingCalls = 0;
			var expectedValue = "1_foo";
			m.Bind(value =>
			{
				bindingCalls++;
				Assert.That(value, Is.EqualTo(expectedValue));
			});
			Assert.That(transformCalls, Is.EqualTo(1));
			Assert.That(bindingCalls, Is.EqualTo(1));

			expectedValue = "1_bar";
			s.Value = "bar";
			Assert.That(transformCalls, Is.EqualTo(2));
			Assert.That(bindingCalls, Is.EqualTo(2));
			Assert.That(m.Value, Is.EqualTo("1_bar"));
			Assert.That(transformCalls, Is.EqualTo(2));

			s.Value = "BAR";
			Assert.That(transformCalls, Is.EqualTo(3));
			Assert.That(bindingCalls, Is.EqualTo(2)); // binding is not called
			Assert.That(m.Value, Is.EqualTo("1_BAR"));
			Assert.That(transformCalls, Is.EqualTo(3));

			expectedValue = "1_BaZ";
			s.Value = "BaZ";
			Assert.That(transformCalls, Is.EqualTo(4));
			Assert.That(bindingCalls, Is.EqualTo(3)); // binding is called
			Assert.That(m.Value, Is.EqualTo("1_BaZ"));
			Assert.That(transformCalls, Is.EqualTo(4));
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