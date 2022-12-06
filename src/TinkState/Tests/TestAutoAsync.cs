using System;
using System.Threading.Tasks;
using NUnit.Framework;
using TinkState;

#pragma warning disable CS1998

namespace Test
{
	class TestAutoAsync : BaseTest
	{
		[Test]
		public void TestSimpleSync()
		{
			var calcCalls = 0;
			var state = Observable.State(1);
			var obs = Observable.Auto(async () =>
			{
				calcCalls++;
				return state.Value * 2;
			});

			var expectedStatus = AsyncComputeStatus.Done;
			var expectedResult = 2;
			Assert.That(obs.Value.Status, Is.EqualTo(expectedStatus));
			Assert.That(obs.Value.Result, Is.EqualTo(expectedResult));
			Assert.That(calcCalls, Is.EqualTo(1));

			var bindingCalls = 0;
			obs.Bind(value =>
			{
				bindingCalls++;
				Assert.That(value.Status, Is.EqualTo(expectedStatus));
				Assert.That(value.Result, Is.EqualTo(expectedResult));
			});
			Assert.That(bindingCalls, Is.EqualTo(1));
			Assert.That(calcCalls, Is.EqualTo(1));

			expectedResult = 6;
			state.Value = 3;
			Assert.That(obs.Value.Status, Is.EqualTo(expectedStatus));
			Assert.That(obs.Value.Result, Is.EqualTo(expectedResult));
			Assert.That(bindingCalls, Is.EqualTo(2));
			Assert.That(calcCalls, Is.EqualTo(2));
		}

		[Test]
		public void TestSimpleAsyncFailure()
		{
			var calcCalls = 0;
			var state = Observable.State(true);
			var obs = Observable.Auto(async () =>
			{
				calcCalls++;
				if (state.Value) throw new Exception("foobar");
				return 10;
			});

			Assert.That(obs.Value.Status, Is.EqualTo(AsyncComputeStatus.Failed));
			Assert.That(obs.Value.Exception, Is.Not.Null);
			Assert.That(obs.Value.Exception.Message, Is.EqualTo("foobar"));
			Assert.That(calcCalls, Is.EqualTo(1));

			var bindingCalls = 0;
			var expectingError = true;
			var expectedResult = 10;
			obs.Bind(value =>
			{
				bindingCalls++;
				if (expectingError)
				{
					Assert.That(value.Status, Is.EqualTo(AsyncComputeStatus.Failed));
					Assert.That(value.Exception, Is.Not.Null);
					Assert.That(value.Exception.Message, Is.EqualTo("foobar"));
				}
				else
				{
					Assert.That(value.Status, Is.EqualTo(AsyncComputeStatus.Done));
					Assert.That(value.Result, Is.EqualTo(expectedResult));
				}
			});
			Assert.That(bindingCalls, Is.EqualTo(1));
			Assert.That(calcCalls, Is.EqualTo(1));

			expectingError = false;
			state.Value = false;
			Assert.That(obs.Value.Status, Is.EqualTo(AsyncComputeStatus.Done));
			Assert.That(obs.Value.Result, Is.EqualTo(expectedResult));
			Assert.That(bindingCalls, Is.EqualTo(2));
			Assert.That(calcCalls, Is.EqualTo(2));
		}

		[Test]
		public void TestAwaitingSyncAsync()
		{
			async ValueTask<int> syncAsync(int v)
			{
				return v * 2;
			}

			var calcCalls = 0;
			var state = Observable.State(1);
			var obs = Observable.Auto(async () =>
			{
				calcCalls++;
				return await syncAsync(state.Value);
			});

			var expectedStatus = AsyncComputeStatus.Done;
			var expectedResult = 2;
			Assert.That(obs.Value.Status, Is.EqualTo(expectedStatus));
			Assert.That(obs.Value.Result, Is.EqualTo(expectedResult));
			Assert.That(calcCalls, Is.EqualTo(1));

			var bindingCalls = 0;
			obs.Bind(value =>
			{
				bindingCalls++;
				Assert.That(value.Status, Is.EqualTo(expectedStatus));
				Assert.That(value.Result, Is.EqualTo(expectedResult));
			});
			Assert.That(bindingCalls, Is.EqualTo(1));
			Assert.That(calcCalls, Is.EqualTo(1));

			expectedResult = 6;
			state.Value = 3;
			Assert.That(obs.Value.Status, Is.EqualTo(expectedStatus));
			Assert.That(obs.Value.Result, Is.EqualTo(expectedResult));
			Assert.That(bindingCalls, Is.EqualTo(2));
			Assert.That(calcCalls, Is.EqualTo(2));
		}

		[Test]
		public void TestAwaitingSyncAsyncFailure()
		{
			async ValueTask<int> syncAsync(bool fail)
			{
				if (fail) throw new Exception("foobar");
				else return 10;
			}

			var calcCalls = 0;
			var state = Observable.State(true);
			var obs = Observable.Auto(async () =>
			{
				calcCalls++;
				return await syncAsync(state.Value);
			});

			Assert.That(obs.Value.Status, Is.EqualTo(AsyncComputeStatus.Failed));
			Assert.That(obs.Value.Exception, Is.Not.Null);
			Assert.That(obs.Value.Exception.Message, Is.EqualTo("foobar"));
			Assert.That(calcCalls, Is.EqualTo(1));

			var bindingCalls = 0;
			var expectingError = true;
			var expectedResult = 10;
			obs.Bind(value =>
			{
				bindingCalls++;
				if (expectingError)
				{
					Assert.That(value.Status, Is.EqualTo(AsyncComputeStatus.Failed));
					Assert.That(value.Exception, Is.Not.Null);
					Assert.That(value.Exception.Message, Is.EqualTo("foobar"));
				}
				else
				{
					Assert.That(value.Status, Is.EqualTo(AsyncComputeStatus.Done));
					Assert.That(value.Result, Is.EqualTo(expectedResult));
				}
			});
			Assert.That(bindingCalls, Is.EqualTo(1));
			Assert.That(calcCalls, Is.EqualTo(1));

			expectingError = false;
			state.Value = false;
			Assert.That(obs.Value.Status, Is.EqualTo(AsyncComputeStatus.Done));
			Assert.That(obs.Value.Result, Is.EqualTo(expectedResult));
			Assert.That(bindingCalls, Is.EqualTo(2));
			Assert.That(calcCalls, Is.EqualTo(2));
		}

		[Test]
		public async Task TestActualAsyncGetValue()
		{
			var calcCalls = 0;
			var stateA = Observable.State(1);
			var stateB = Observable.State(2);
			var obs = Observable.Auto(async () =>
			{
				calcCalls++;
				var a = stateA.Value;
				await Task.Delay(10);
				var b = stateB.Value; // also make sure that access is tracked on each state machine step
				return a + b;
			});

			Assert.That(obs.Value.Status, Is.EqualTo(AsyncComputeStatus.Loading));
			Assert.That(calcCalls, Is.EqualTo(1));

			Assert.That(obs.Value.Status, Is.EqualTo(AsyncComputeStatus.Loading));
			Assert.That(calcCalls, Is.EqualTo(1));

			await Task.Delay(50);

			Assert.That(obs.Value.Status, Is.EqualTo(AsyncComputeStatus.Done));
			Assert.That(obs.Value.Result, Is.EqualTo(3));
			Assert.That(calcCalls, Is.EqualTo(1));

			// trigger reload
			stateA.Value = 3;
			Assert.That(obs.Value.Status, Is.EqualTo(AsyncComputeStatus.Loading));
			Assert.That(calcCalls, Is.EqualTo(2));
			await Task.Delay(50);
			Assert.That(obs.Value.Status, Is.EqualTo(AsyncComputeStatus.Done));
			Assert.That(obs.Value.Result, Is.EqualTo(5));
			Assert.That(calcCalls, Is.EqualTo(2));

			// post-await block should also track properly
			stateB.Value = 4;
			Assert.That(obs.Value.Status, Is.EqualTo(AsyncComputeStatus.Loading));
			Assert.That(calcCalls, Is.EqualTo(3));
			await Task.Delay(50);
			Assert.That(obs.Value.Status, Is.EqualTo(AsyncComputeStatus.Done));
			Assert.That(obs.Value.Result, Is.EqualTo(7));
			Assert.That(calcCalls, Is.EqualTo(3));
		}

		[Test]
		public async Task TestActualAsyncBind()
		{
			var calcCalls = 0;
			var stateA = Observable.State(1);
			var stateB = Observable.State(2);
			var obs = Observable.Auto(async () =>
			{
				calcCalls++;
				var a = stateA.Value;
				await Task.Delay(10);
				var b = stateB.Value; // also make sure that access is tracked on each state machine step
				return a + b;
			});

			var bindingCalls = 0;
			Action<AsyncComputeResult<int>> expect = value => { Assert.That(value.Status, Is.EqualTo(AsyncComputeStatus.Loading)); };
			var binding = obs.Bind(value =>
			{
				bindingCalls++;
				expect(value);
			});

			Assert.That(bindingCalls, Is.EqualTo(1));
			Assert.That(calcCalls, Is.EqualTo(1));

			expect = value =>
			{
				Assert.That(value.Status, Is.EqualTo(AsyncComputeStatus.Done));
				Assert.That(value.Result, Is.EqualTo(3));
			};

			await Task.Delay(50);

			Assert.That(bindingCalls, Is.EqualTo(2));
			Assert.That(calcCalls, Is.EqualTo(1));

			// trigger reload
			expect = value => { Assert.That(value.Status, Is.EqualTo(AsyncComputeStatus.Loading)); };
			stateA.Value = 3;
			Assert.That(bindingCalls, Is.EqualTo(3));
			Assert.That(calcCalls, Is.EqualTo(2));

			expect = value =>
			{
				Assert.That(value.Status, Is.EqualTo(AsyncComputeStatus.Done));
				Assert.That(value.Result, Is.EqualTo(5));
			};
			await Task.Delay(50);
			Assert.That(bindingCalls, Is.EqualTo(4));
			Assert.That(calcCalls, Is.EqualTo(2));


			// post-await block should also track properly
			expect = value => { Assert.That(value.Status, Is.EqualTo(AsyncComputeStatus.Loading)); };
			stateB.Value = 4;
			Assert.That(bindingCalls, Is.EqualTo(5));
			Assert.That(calcCalls, Is.EqualTo(3));

			expect = value =>
			{
				Assert.That(value.Status, Is.EqualTo(AsyncComputeStatus.Done));
				Assert.That(value.Result, Is.EqualTo(7));
			};
			await Task.Delay(50);
			Assert.That(bindingCalls, Is.EqualTo(6));
			Assert.That(calcCalls, Is.EqualTo(3));

			binding.Dispose();
			// no new binding or compute calls after disposal
			stateA.Value = 0;
			stateB.Value = 0;
			Assert.That(bindingCalls, Is.EqualTo(6));
			Assert.That(calcCalls, Is.EqualTo(3));
		}

		[Test]
		public async Task TestActualAsyncFailureGetValue()
		{
			var calcCalls = 0;
			var stateA = Observable.State(1);
			var stateB = Observable.State(true);
			var obs = Observable.Auto(async () =>
			{
				calcCalls++;
				var a = stateA.Value;
				await Task.Delay(10);
				if (stateB.Value) throw new Exception("foobar");
				return a;
			});

			Assert.That(obs.Value.Status, Is.EqualTo(AsyncComputeStatus.Loading));
			Assert.That(calcCalls, Is.EqualTo(1));

			Assert.That(obs.Value.Status, Is.EqualTo(AsyncComputeStatus.Loading));
			Assert.That(calcCalls, Is.EqualTo(1));

			await Task.Delay(50);

			Assert.That(obs.Value.Status, Is.EqualTo(AsyncComputeStatus.Failed));
			Assert.That(obs.Value.Exception, Is.Not.Null);
			Assert.That(obs.Value.Exception.Message, Is.EqualTo("foobar"));
			Assert.That(calcCalls, Is.EqualTo(1));

			// trigger reload
			stateA.Value = 3;
			Assert.That(obs.Value.Status, Is.EqualTo(AsyncComputeStatus.Loading));
			Assert.That(calcCalls, Is.EqualTo(2));
			await Task.Delay(50);
			Assert.That(obs.Value.Status, Is.EqualTo(AsyncComputeStatus.Failed));
			Assert.That(obs.Value.Exception, Is.Not.Null);
			Assert.That(obs.Value.Exception.Message, Is.EqualTo("foobar"));
			Assert.That(calcCalls, Is.EqualTo(2));

			// post-await block should also track properly
			stateB.Value = false;
			Assert.That(obs.Value.Status, Is.EqualTo(AsyncComputeStatus.Loading));
			Assert.That(calcCalls, Is.EqualTo(3));
			await Task.Delay(50);
			Assert.That(obs.Value.Status, Is.EqualTo(AsyncComputeStatus.Done));
			Assert.That(obs.Value.Result, Is.EqualTo(3));
			Assert.That(calcCalls, Is.EqualTo(3));
		}

		[Test]
		public async Task TestActualAsyncFailureBind()
		{
			var calcCalls = 0;
			var stateA = Observable.State(1);
			var stateB = Observable.State(false);
			var obs = Observable.Auto(async () =>
			{
				calcCalls++;
				var a = stateA.Value;
				await Task.Delay(10);
				if (stateB.Value) throw new Exception("foobar");
				return a;
			});

			var bindingCalls = 0;
			Action<AsyncComputeResult<int>> expect = value => { Assert.That(value.Status, Is.EqualTo(AsyncComputeStatus.Loading)); };
			var binding = obs.Bind(value =>
			{
				bindingCalls++;
				expect(value);
			});

			Assert.That(bindingCalls, Is.EqualTo(1));
			Assert.That(calcCalls, Is.EqualTo(1));

			expect = value =>
			{
				Assert.That(value.Status, Is.EqualTo(AsyncComputeStatus.Done));
				Assert.That(value.Result, Is.EqualTo(1));
			};

			await Task.Delay(50);

			Assert.That(bindingCalls, Is.EqualTo(2));
			Assert.That(calcCalls, Is.EqualTo(1));

			// trigger reload
			expect = value => { Assert.That(value.Status, Is.EqualTo(AsyncComputeStatus.Loading)); };
			stateA.Value = 3;
			Assert.That(bindingCalls, Is.EqualTo(3));
			Assert.That(calcCalls, Is.EqualTo(2));

			expect = value =>
			{
				Assert.That(value.Status, Is.EqualTo(AsyncComputeStatus.Done));
				Assert.That(value.Result, Is.EqualTo(3));
			};
			await Task.Delay(50);
			Assert.That(bindingCalls, Is.EqualTo(4));
			Assert.That(calcCalls, Is.EqualTo(2));


			// post-await block should also track properly
			expect = value => { Assert.That(value.Status, Is.EqualTo(AsyncComputeStatus.Loading)); };
			stateB.Value = true;
			Assert.That(bindingCalls, Is.EqualTo(5));
			Assert.That(calcCalls, Is.EqualTo(3));

			expect = value =>
			{
				Assert.That(value.Status, Is.EqualTo(AsyncComputeStatus.Failed));
				Assert.That(value.Exception, Is.Not.Null);
				Assert.That(value.Exception.Message, Is.EqualTo("foobar"));
			};
			await Task.Delay(50);
			Assert.That(bindingCalls, Is.EqualTo(6));
			Assert.That(calcCalls, Is.EqualTo(3));

			binding.Dispose();
			// no new binding or compute calls after disposal
			stateA.Value = 0;
			stateB.Value = true;
			Assert.That(bindingCalls, Is.EqualTo(6));
			Assert.That(calcCalls, Is.EqualTo(3));
		}

		[Test]
		public async Task TestNoSubscriptionsAnymoreAsync()
		{
			// this example is rather artificial, as the computation code shouldn't depend on non-observable mutable values,
			// but who knows, maybe someone will come up with some use-case for that. the test here basically covers the case
			// where a binding should be disposed if its observable will never fire anymore after the last recomputation
			var s = Observable.State(10);
			var track = true;
			var o = Observable.Auto(async () =>
			{
				await Task.Delay(10);
				if (track) return s.Value + 1;
				else return 42;
			});

			var bindingCalls = 0;
			Action<AsyncComputeResult<int>> expect = value => { Assert.That(value.Status, Is.EqualTo(AsyncComputeStatus.Loading)); };
			o.Bind(value =>
			{
				bindingCalls++;
				expect(value);
			});
			Assert.That(bindingCalls, Is.EqualTo(1));

			expect = value =>
			{
				Assert.That(value.Status, Is.EqualTo(AsyncComputeStatus.Done));
				Assert.That(value.Result, Is.EqualTo(11));
			};
			await Task.Delay(50);
			Assert.That(bindingCalls, Is.EqualTo(2));

			expect = value => { Assert.That(value.Status, Is.EqualTo(AsyncComputeStatus.Loading)); };
			track = false;
			s.Value++;
			Assert.That(bindingCalls, Is.EqualTo(3));

			expect = value =>
			{
				Assert.That(value.Status, Is.EqualTo(AsyncComputeStatus.Done));
				Assert.That(value.Result, Is.EqualTo(42));
			};
			await Task.Delay(50);
			Assert.That(bindingCalls, Is.EqualTo(4));

			s.Value++;
			Assert.That(bindingCalls, Is.EqualTo(4));
		}

		[Test]
		public void TestNoSubscriptionsAnymoreSync()
		{
			// this example is rather artificial, as the computation code shouldn't depend on non-observable mutable values,
			// but who knows, maybe someone will come up with some use-case for that. the test here basically covers the case
			// where a binding should be disposed if its observable will never fire anymore after the last recomputation
			var s = Observable.State(10);
			var track = true;
			var o = Observable.Auto(async () =>
			{
				if (track) return s.Value + 1;
				else return 42;
			});

			var bindingCalls = 0;
			var expectedValue = 11;
			o.Bind(value =>
			{
				bindingCalls++;
				Assert.That(value.Status, Is.EqualTo(AsyncComputeStatus.Done));
				Assert.That(value.Result, Is.EqualTo(expectedValue));
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
		public async Task TestChangingSourceWhileComputing()
		{
			var s = Observable.State(1);
			var computeCalls = 0;
			var o = Observable.Auto(async () =>
			{
				computeCalls++;
				var v = s.Value;
				await Task.Delay(50);
				return v * 2;
			});

			Assert.That(computeCalls, Is.Zero);
			Assert.That(o.Value.Status, Is.EqualTo(AsyncComputeStatus.Loading));
			Assert.That(computeCalls, Is.EqualTo(1));

			await Task.Delay(10);

			// still computing
			Assert.That(computeCalls, Is.EqualTo(1));
			Assert.That(o.Value.Status, Is.EqualTo(AsyncComputeStatus.Loading));
			Assert.That(computeCalls, Is.EqualTo(1));

			s.Value++;

			// new computation triggered
			Assert.That(computeCalls, Is.EqualTo(1));
			Assert.That(o.Value.Status, Is.EqualTo(AsyncComputeStatus.Loading));
			Assert.That(computeCalls, Is.EqualTo(2));

			await Task.Delay(100);

			Assert.That(computeCalls, Is.EqualTo(2));
			Assert.That(o.Value.Status, Is.EqualTo(AsyncComputeStatus.Done));
			Assert.That(o.Value.Result, Is.EqualTo(4));
			Assert.That(computeCalls, Is.EqualTo(2));
		}

		[Test]
		public async Task TestMapAsync()
		{
			var s = Observable.State(1);
			var o = Observable.Auto(async () =>
			{
				var v = s.Value;
				await Task.Delay(10);
				if (v == 5) throw new Exception("Fail");
				return v * 2;
			});
			var transformCalls = 0;
			var o2 = o.Map(result =>
			{
				transformCalls++;
				return result.Map(v => "foo_" + v);
			});

			Assert.That(transformCalls, Is.Zero);
			Assert.That(o2.Value.Status, Is.EqualTo(AsyncComputeStatus.Loading));
			Assert.That(transformCalls, Is.EqualTo(1));

			await Task.Delay(50);
			Assert.That(transformCalls, Is.EqualTo(1));
			Assert.That(o2.Value.Status, Is.EqualTo(AsyncComputeStatus.Done));
			Assert.That(o2.Value.Result, Is.EqualTo("foo_2"));
			Assert.That(transformCalls, Is.EqualTo(2));

			s.Value++;
			Assert.That(transformCalls, Is.EqualTo(2));
			Assert.That(o2.Value.Status, Is.EqualTo(AsyncComputeStatus.Loading));
			Assert.That(transformCalls, Is.EqualTo(3));

			await Task.Delay(50);

			Assert.That(transformCalls, Is.EqualTo(3));
			Assert.That(o2.Value.Status, Is.EqualTo(AsyncComputeStatus.Done));
			Assert.That(o2.Value.Result, Is.EqualTo("foo_4"));
			Assert.That(transformCalls, Is.EqualTo(4));

			s.Value = 5;

			Assert.That(transformCalls, Is.EqualTo(4));
			Assert.That(o2.Value.Status, Is.EqualTo(AsyncComputeStatus.Loading));
			Assert.That(transformCalls, Is.EqualTo(5));

			await Task.Delay(50);

			Assert.That(transformCalls, Is.EqualTo(5));
			Assert.That(o2.Value.Status, Is.EqualTo(AsyncComputeStatus.Failed));
			Assert.That(o2.Value.Exception, Is.Not.Null);
			Assert.That(o2.Value.Exception.Message, Is.EqualTo("Fail"));
			Assert.That(transformCalls, Is.EqualTo(6));
		}
	}
}