using NUnit.Framework;
using TinkState;

namespace Test
{
	// since our binding tests currently rely on the direct scheduler
	// we want to explicitly set it to be direct for the test in case
	// we're running from an environment that provides batching (e.g. Unity)
	class BaseTest
	{
		Scheduler prevScheduler;

		[SetUp]
		public void SetUp()
		{
			prevScheduler = Observable.Scheduler;
			Observable.Scheduler = Scheduler.Direct;
		}

		[TearDown]
		public void TearDown()
		{
			Observable.Scheduler = prevScheduler;
			prevScheduler = null;
		}
	}
}
