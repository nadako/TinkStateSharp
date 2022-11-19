using System;
using System.Collections.Generic;
using NUnit.Framework;
using TinkState;

namespace Test
{
	class TestList : BaseTest
	{
		[Test]
		public void TestIsReadonly()
		{
			var list = Observable.List<int>();
			Assert.That(list.IsReadOnly, Is.False);
		}

		[Test]
		public void TestInitialValue()
		{
			var list = Observable.List(new[] { 1, 2, 3 });
			Assert.That(string.Join(",", list), Is.EqualTo("1,2,3"));
		}

		[Test]
		public void TestEnumeration()
		{
			var list = Observable.List<int>();

			Helper(() => string.Join(",", list), "", expect =>
			{
				// test all invalidating methods here
				expect("10", () => list.Add(10));
				expect("10,12", () => list.Add(12));
				expect("11,12", () => list[0] = 11);
				expect("11,10,12", () => list.Insert(1, 10));
				expect("11,12", () => list.RemoveAt(1));
				expect("11", () => list.Remove(12));
				expect("", () => list.Clear());
			});

			Helper(() =>
			{
				var newList = new List<int>();
				var e = ((System.Collections.IEnumerable)list).GetEnumerator();
				while (e.MoveNext())
				{
					newList.Add((int)e.Current);
				}
				return string.Join(",", newList);
			}, "", expect =>
			{
				// test all invalidating methods here
				expect("10", () => list.Add(10));
				expect("10,12", () => list.Add(12));
				expect("11,12", () => list[0] = 11);
				expect("11,10,12", () => list.Insert(1, 10));
				expect("11,12", () => list.RemoveAt(1));
				expect("11", () => list.Remove(12));
				expect("", () => list.Clear());
			});
		}

		[Test]
		public void TestCount()
		{
			var list = Observable.List<int>();
			Helper(() => list.Count, 0, expect =>
			{
				expect(1, () => list.Add(12));
				expect(2, () => list.Add(10));
				expect(0, () => list.Clear());
			});
		}

		[Test]
		public void TestGetItem()
		{
			var list = Observable.List<int>();
			list.Add(1);
			list.Add(2);
			list.Add(3);
			Helper(() => list[1], 2, expect =>
			{
				expect(10, () => list[1] = 10);
				expect(3, () => list.Remove(10));
			});
		}

		[Test]
		public void TestContains()
		{
			var list = Observable.List<int>();
			list.Add(1);
			list.Add(2);
			list.Add(3);
			Helper(() => list.Contains(2), true, expect =>
			{
				expect(false, () => list[1] = 10);
				expect(true, () => list[1] = 2);
				expect(false, () => list.Clear());
			});
		}

		[Test]
		public void TestIndexOf()
		{
			var list = Observable.List<int>();
			list.Add(1);
			list.Add(2);
			list.Add(3);
			Helper(() => list.IndexOf(2), 1, expect =>
			{
				expect(-1, () => list[1] = 10);
				expect(1, () => list[1] = 2);
				expect(-1, () => list.Clear());
			});
		}

		[Test]
		public void TestCopyTo()
		{
			var list = Observable.List<int>();
			list.Add(1);
			list.Add(2);
			list.Add(3);
			Helper(() =>
			{
				int[] dest = new int[list.Count];
				list.CopyTo(dest, 0);
				return string.Join(",", dest);
			}, "1,2,3", expect =>
			{
				expect("1,10,3", () => list[1] = 10);
				expect("1,2,3", () => list[1] = 2);
				expect("", () => list.Clear());
			});
		}

		void Helper<R>(Func<R> compute, R initialExpectedValue, Action<Action<R, Action>> tests)
		{
			var auto = Observable.Auto(compute);
			R expectedValue = initialExpectedValue;
			var bindingCalled = false;
			var binding = auto.Bind(value =>
			{
				bindingCalled = true;
				Assert.That(value, Is.EqualTo(expectedValue));
			});
			Assert.That(bindingCalled, Is.True);

			void Expect(R newExpectedValue, Action f)
			{
				bindingCalled = false;
				expectedValue = newExpectedValue;
				f();
				Assert.That(bindingCalled, Is.True);
			};

			tests(Expect);
			binding.Dispose();
		}
	}
}
