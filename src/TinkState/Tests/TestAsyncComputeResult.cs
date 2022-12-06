using System;
using NUnit.Framework;
using TinkState;

namespace Test
{
	public class TestAsyncComputeResult
	{
		[Test]
		public void TestDefault()
		{
			var result = default(AsyncComputeResult<int>);
			Assert.That(result.Status, Is.EqualTo(AsyncComputeStatus.Loading));
			Assert.That(result.Result, Is.EqualTo(0));
			Assert.That(result.Exception, Is.Null);
		}

		[Test]
		public void TestCreateLoading()
		{
			var result = AsyncComputeResult<int>.CreateLoading();
			Assert.That(result.Status, Is.EqualTo(AsyncComputeStatus.Loading));
			Assert.That(result.Result, Is.EqualTo(0));
			Assert.That(result.Exception, Is.Null);
		}

		[Test]
		public void TestCreateDone()
		{
			var result = AsyncComputeResult<int>.CreateDone(42);
			Assert.That(result.Status, Is.EqualTo(AsyncComputeStatus.Done));
			Assert.That(result.Result, Is.EqualTo(42));
			Assert.That(result.Exception, Is.Null);
		}

		[Test]
		public void TestCreateFailed()
		{
			var result = AsyncComputeResult<int>.CreateFailed(new Exception("FAIL"));
			Assert.That(result.Status, Is.EqualTo(AsyncComputeStatus.Failed));
			Assert.That(result.Result, Is.EqualTo(0));
			Assert.That(result.Exception, Is.Not.Null);
			Assert.That(result.Exception.Message, Is.EqualTo("FAIL"));
		}
	}
}