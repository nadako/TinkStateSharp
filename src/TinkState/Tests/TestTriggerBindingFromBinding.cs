using NUnit.Framework;
using TinkState;

namespace Test
{
	// NOTE: this is not an intended usage for bindings, as one shouldn't
	// trigger bindings from other bindings (e.g. by changing a State) and
	// instead make use of auto-Observables and such, but we still test
	// that this works as expected and doesn't hang
	class TestTriggerBindingFromBinding : BaseTest
	{
		[Test]
		public void Test()
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

			expectedValue = 4;
			s.Bind(v => s2.Value = v * 2);
			Assert.That(bindingCalls, Is.EqualTo(2));

			expectedValue = 6;
			s.Value = 3;
			Assert.That(bindingCalls, Is.EqualTo(3));
		}
	}
}