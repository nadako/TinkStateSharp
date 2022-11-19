using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using TinkState;

namespace Test
{
	class TestBinding : BaseTest
	{
		[Test]
		public void TestCustomComparer()
		{
			var state = Observable.State("Dan");

			var expectedValue = "Dan";
			var bindingCalls = 0;
			state.Bind(value =>
			{
				bindingCalls++;
				Assert.That(value, Is.EqualTo(expectedValue));
			}, new CustomComparer());
			Assert.That(bindingCalls, Is.EqualTo(1));

			state.Value = "Dan";
			Assert.That(bindingCalls, Is.EqualTo(1));

			state.Value = "DAN";
			Assert.That(bindingCalls, Is.EqualTo(1));
			Assert.That(state.Value, Is.EqualTo("DAN"));

			state.Value = expectedValue = "John";
			Assert.That(bindingCalls, Is.EqualTo(2));
		}
	}
}
