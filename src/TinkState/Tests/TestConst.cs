using NUnit.Framework;
using TinkState;

namespace Test
{
	class TestConst : BaseTest
	{
		[Test]
		public void Test()
		{
			var o = Observable.Const(42);

			Assert.That(o.Value, Is.EqualTo(42));

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
		public void TestMap()
		{
			var o = Observable.Const(42);

			var transformCalls = 0;
			var m = o.Map(value =>
			{
				transformCalls++;
				return (value * 2).ToString();
			});

			Assert.That(transformCalls, Is.Zero);
			Assert.That(m.Value, Is.EqualTo("84"));
			Assert.That(transformCalls, Is.EqualTo(1));
			Assert.That(m.Value, Is.EqualTo("84"));
			Assert.That(transformCalls, Is.EqualTo(1));

			var bindingCalls = 0;
			m.Bind(value =>
			{
				bindingCalls++;
				Assert.That(value, Is.EqualTo("84"));
			});
			Assert.That(bindingCalls, Is.EqualTo(1));
			Assert.That(transformCalls, Is.EqualTo(1));

			var transform2Calls = 0;
			var m2 = m.Map(value =>
			{
				transform2Calls++;
				return "foo_" + value;
			});
			Assert.That(transformCalls, Is.EqualTo(1));
			Assert.That(transform2Calls, Is.Zero);

			var binding2Calls = 0;
			m2.Bind(value =>
			{
				binding2Calls++;
				Assert.That(value, Is.EqualTo("foo_84"));
			});
			Assert.That(binding2Calls, Is.EqualTo(1));
			Assert.That(transformCalls, Is.EqualTo(1));
			Assert.That(transform2Calls, Is.EqualTo(1));

			Assert.That(m2.Value, Is.EqualTo("foo_84"));
			Assert.That(transformCalls, Is.EqualTo(1));
			Assert.That(transform2Calls, Is.EqualTo(1));
		}
	}
}
