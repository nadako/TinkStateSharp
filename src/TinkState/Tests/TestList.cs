﻿using System;
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

		[Test]
		public void TestObserve()
		{
			var list = Observable.List<int>();
			list.Add(1);
			list.Add(2);
			list.Add(3);
			var observable = list.Observe();
			Assert.That(string.Join(",", observable.Value), Is.EqualTo("1,2,3"));
			list.Add(4);
			Assert.That(string.Join(",", observable.Value), Is.EqualTo("1,2,3,4"));
			list.Remove(2);
			Assert.That(string.Join(",", observable.Value), Is.EqualTo("1,3,4"));

			var bindingCalls = 0;
			var expectedValue = "1,3,4";
			var binding = observable.Bind(list =>
			{
				bindingCalls++;
				Assert.That(string.Join(",", list), Is.EqualTo(expectedValue));
			});
			Assert.That(bindingCalls, Is.EqualTo(1));

			expectedValue = "1,2,3,4";
			list.Insert(1, 2);
			Assert.That(bindingCalls, Is.EqualTo(2));

			binding.Dispose();
			list.Insert(0, 0);
			Assert.That(bindingCalls, Is.EqualTo(2));

			var mapped = observable.Map(list => string.Join(",", list));
			Assert.That(mapped.Value, Is.EqualTo("0,1,2,3,4"));

			var auto = Observable.Auto(() => string.Join(",", observable.Value));
			Assert.That(auto.Value, Is.EqualTo("0,1,2,3,4"));
			list.Remove(4);
			Assert.That(auto.Value, Is.EqualTo("0,1,2,3"));
		}


		[Test]
		public void TestObserveAndListUsedInAuto()
		{
			var list = Observable.List<string>();
			list.Add("a");
			list.Add("b");
			var listAsObservable = list.Observe();

			var auto = Observable.Auto(() =>
			{
				var a = string.Join(',', list);
				var b = string.Join('.', listAsObservable.Value);
				return $"{a}-{b}";
			});

			Assert.That(auto.Value, Is.EqualTo("a,b-a.b"));

			list[0] = "A";
			list[1] = "B";
			list.Add("C");
			Assert.That(auto.Value, Is.EqualTo("A,B,C-A.B.C"));
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
