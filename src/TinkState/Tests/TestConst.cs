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
	}
}
